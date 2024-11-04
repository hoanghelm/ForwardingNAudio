using WebSocketSharp;
using System;
using Newtonsoft.Json;
using System.Text;

public class WebSocketSignaler
{
	public Action<string> OnMessage { get; set; }
	private WebSocket _webSocket;

	public void Connect(string signalingServerUrl)
	{
		_webSocket = new WebSocket(signalingServerUrl);
		_webSocket.OnMessage += (sender, e) =>
		{
			byte[] binaryData = e.RawData;
			string jsonData = Encoding.UTF8.GetString(binaryData);
			Console.WriteLine($"Received message from server: {jsonData}");
			OnMessage.Invoke(jsonData);
		};

		_webSocket.Connect();
		Console.WriteLine("Connected to signaling server.");
	}

	public void SendMessage(string message)
	{
		if (_webSocket.IsAlive)
		{
			_webSocket.Send(message);
			Console.WriteLine($"Sent message: {message}");
		}
		else
		{
			Console.WriteLine("WebSocket is not alive. Unable to send message.");
		}
	}
}

