using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using Newtonsoft.Json;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;

public class NAudioFileStreamer
{
	private WaveFileReader _waveFileReader;
	private readonly WebRTCStreamer _webRTCStreamer;
	private int _audioSampleRate;
	private string _filePath;

	public NAudioFileStreamer(WebSocketSignaler signaler, string filePath)
	{
		_filePath = filePath;
		_webRTCStreamer = new WebRTCStreamer(signaler);
		_webRTCStreamer.InitializeWebRTC();
		signaler.OnMessage += (data) =>
		{
			try
			{
				var session = JsonConvert.DeserializeObject<RTCSessionDescriptionInit>(data);
				if (session.sdp != null)
				{
					_webRTCStreamer.SetRemoteDescription(session);
				}
				else 
				{
					var iceDescription = JsonConvert.DeserializeObject<RTCIceCandidateInit>(data);
					_webRTCStreamer.AddCandidate(iceDescription);
				}
			} 
			catch (Exception ex) 
			{
				Console.WriteLine($"Error setting remote description: {ex.Message}");
			}
		};
		_webRTCStreamer.OnConnectionStatusChanged += (state) =>
		{
			switch (state)
			{
				case RTCPeerConnectionState.connected:
					//_webRTCStreamer.StartAudio();
					StartStreaming(_filePath);
					break;
				case RTCPeerConnectionState.disconnected:
					_webRTCStreamer.Dispose();
					break;
				case RTCPeerConnectionState.closed:
					//_webRTCStreamer.CloseAudio();
					StopStreaming();
					break;
			}
		};
	}

	public void StartStreaming(string filePath)
	{
		if (!File.Exists(filePath))
		{
			Console.WriteLine($"Audio file not found: {filePath}");
			return;
		}

		// Define PCM and µ-law formats
		var pcmFormat = new WaveFormat(8000, 16, 1);  // PCM format: 8000 Hz, 16-bit, mono
		var ulawFormat = WaveFormat.CreateMuLawFormat(8000, 1);  // µ-law format: 8000 Hz, mono

		try
		{
			using (var mfReader = new MediaFoundationReader(filePath))
			using (var pcmStream = new WaveFormatConversionStream(pcmFormat, mfReader))
			using (var ulawStream = new WaveFormatConversionStream(ulawFormat, pcmStream))
			{
				Console.WriteLine("Starting audio streaming...");
				_webRTCStreamer.StartAudio();

				byte[] buffer = new byte[160];
				int bytesRead;
				uint rtpTimestamp = 0;
				const uint durationRtpUnits = 160;

				while ((bytesRead = ulawStream.Read(buffer, 0, buffer.Length)) > 0)
				{
					if (bytesRead < buffer.Length)
					{
						Array.Clear(buffer, bytesRead, buffer.Length - bytesRead);
					}

					_webRTCStreamer.SendAudio(durationRtpUnits, buffer);
					rtpTimestamp += durationRtpUnits;

					System.Threading.Thread.Sleep(20);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error streaming audio: {ex.Message}");
		}
		finally
		{
			StopStreaming();
		}
	}

	public void StopStreaming()
	{
		_webRTCStreamer.CloseAudio();
		_waveFileReader?.Dispose();
		_webRTCStreamer.Dispose();
		Console.WriteLine("Streaming stopped.");
	}
}