using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using SIPSorceryMedia.Encoders;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using SIPSorcery.net.RTP;
using static WebSocketSignaler;

public class WebRTCStreamer
{
	public Action<RTCPeerConnectionState> OnConnectionStatusChanged { get; set; }
	private RTCPeerConnection _peerConnection;
	private AudioExtrasSource _audioSource;
	private WebSocketSignaler _signaler;

	public WebRTCStreamer(WebSocketSignaler signaler)
	{
		_signaler = signaler;
	}

	public void InitializeWebRTC()
	{
		var config = new RTCConfiguration
		{
			iceServers = new List<RTCIceServer> { new RTCIceServer { urls = "stun:stun.sipsorcery.com" } }
		};
		_peerConnection = new RTCPeerConnection(config);

		_audioSource = new AudioExtrasSource(new AudioEncoder(), new AudioSourceOptions { AudioSource = AudioSourcesEnum.Music });
		//_audioSource.OnAudioSourceEncodedSample += SendAudio;
		MediaStreamTrack audioTrack = new MediaStreamTrack(_audioSource.GetAudioSourceFormats(), MediaStreamStatusEnum.SendOnly);
		_peerConnection.addTrack(audioTrack);
		_peerConnection.OnAudioFormatsNegotiated += (audioFormats) => _audioSource.SetAudioSourceFormat(audioFormats.First());

		_peerConnection.onicecandidate += (iceCandidate) =>
		{
			if (iceCandidate != null )
			{
				Console.WriteLine("New ICE candidate available.");
				_signaler.SendMessage(JsonConvert.SerializeObject(new { ice = iceCandidate }));
			}
		};

		_peerConnection.onconnectionstatechange += async (state) =>
		{
			Console.WriteLine($"Peer connection state change to {state}.");
			OnConnectionStatusChanged.Invoke(state);
		};

		CreateOfferAsync();

		Console.WriteLine("WebRTC initialized.");
	}

	public void StartAudio()
	{
		_audioSource.StartAudio();
	}

	public void CloseAudio()
	{
		_audioSource.Close();
	}

	public void SendAudio(uint durationRtpUnits, byte[] sample)
	{
		_peerConnection.SendAudio(durationRtpUnits, sample);
	}

	public async Task CreateOfferAsync()
	{
		var offer = _peerConnection.createOffer();
		await _peerConnection.setLocalDescription(offer);
		_signaler.SendMessage(JsonConvert.SerializeObject(new { sdp = _peerConnection.localDescription }));
	}

	public void AddCandidate(RTCIceCandidateInit init) 
	{
		if (init == null || init.candidate == null) return;

		try
		{
			_peerConnection.addIceCandidate(init);
			Console.WriteLine("Add ice candidate successfully.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error add ice candidate: {ex.Message}");
		}
	}

	public void SetRemoteDescription(RTCSessionDescriptionInit init)
	{
		if (init == null || init.sdp == null) return;

		try
		{
			_peerConnection.setRemoteDescription(init);
			Console.WriteLine("Remote SDP answer set successfully.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error setting remote description: {ex.Message}");
		}
	}


	public void Dispose()
	{
		_audioSource.CloseAudio();
		_peerConnection.Dispose();
	}
}
