// See https://aka.ms/new-console-template for more information

var signaler = new WebSocketSignaler();
signaler.Connect("ws://localhost:8080");

string filePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "resources/audio/sample.wav");
var naudioFileStreamer = new NAudioFileStreamer(signaler, filePath);

Console.WriteLine("Press any key to stop...");
Console.ReadKey();
