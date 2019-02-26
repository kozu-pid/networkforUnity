using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;

public class SocketSampleTCP : MonoBehaviour
{
	
	// 接続先のIPアドレス.
	private string			m_address = "";
	
	// 接続先のポート番号.
	private const int 		m_port = 50765;

	// リスニングソケット.
	private Socket			m_listener = null;

	// 通信用変数
	private Socket			m_socket = null;

	// 状態.
	private State			m_state;

	// 状態定義
	private enum State
	{
		SelectHost = 0,
		StartListener,
		AcceptClient,
		ServerCommunication,
		StopListener,
		ClientCommunication,
		Endcommunication,
	}


	// Use this for initialization
	void Start ()
	{
		m_state = State.SelectHost;

		IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
		System.Net.IPAddress hostAddress = hostEntry.AddressList[0];
		Debug.Log(hostEntry.HostName);
		m_address = hostAddress.ToString();
	}
	
	// Update is called once per frame
	void Update ()
	{
		switch (m_state) {
		case State.StartListener:
			StartListener();
			break;

		case State.AcceptClient:
			AcceptClient();
			break;

		case State.ServerCommunication:
			ServerCommunication();
			break;

		case State.StopListener:
			StopListener();
			break;

		case State.ClientCommunication:
			ClientProcess();
			break;

		default:
			break;
		}
	}

	// 待ち受け開始.
	void StartListener()
	{
		Debug.Log("Start server communication.");
		
		// ソケットを生成します.
		m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		// 使用するポート番号を割り当てます.
		m_listener.Bind(new IPEndPoint(IPAddress.Any, m_port));
		// 待ち受けを開始します.
		m_listener.Listen(1);

		m_state = State.AcceptClient;
	}

	// クライアントからの接続待ち.P39
	void AcceptClient()
	{
		if (m_listener != null && m_listener.Poll(0, SelectMode.SelectRead)) {
			// クライアントから接続されました.
			m_socket = m_listener.Accept();
			Debug.Log("[TCP]Connected from client.");
			m_state = State.ServerCommunication;
		}
	}

	// クライアントからのメッセージ受信.P38
	void ServerCommunication()
	{
		byte[] buffer = new byte[1400];
		int recvSize = m_socket.Receive(buffer, buffer.Length, SocketFlags.None);
		if (recvSize > 0) {
			string message = System.Text.Encoding.UTF8.GetString(buffer);
			Debug.Log(message);
			m_state = State.StopListener;
		}
	}

	// 待ち受け終了.
	void StopListener()
	{	
		// 待ち受けを終了します.
		if (m_listener != null) {
			m_listener.Close();
			m_listener = null;
		}

		m_state = State.Endcommunication;

		Debug.Log("[TCP]End server communication.");
	}

	// クライアントとの接続, 送信, 切断.
	void ClientProcess()
	{
		Debug.Log("[TCP]Start client communication.");

		// サーバへ接続.
		m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		m_socket.NoDelay = true;
		m_socket.SendBufferSize = 0;
		m_socket.Connect(m_address, m_port);

		// メッセージ送信.
		byte[] buffer = System.Text.Encoding.UTF8.GetBytes("Hello, this is client.");	
		m_socket.Send(buffer, buffer.Length, SocketFlags.None);

		// 切断.
		m_socket.Shutdown(SocketShutdown.Both);
		m_socket.Close();

		Debug.Log("[TCP]End client communication.");
	}

	void OnGUI()
	{
		if (m_state == State.SelectHost) {
			OnGUISelectHost();
		}
	}

	void OnGUISelectHost()
	{
		if (GUI.Button (new Rect (20,40, 150,20), "Launch server.")) {
			m_state = State.StartListener;
		}
		
		// クライアントを選択した時の接続するサーバのアドレスを入力します.
		m_address = GUI.TextField(new Rect(20, 100, 200, 20), m_address);
		if (GUI.Button (new Rect (20,70,150,20), "Connect to server")) {
			m_state = State.ClientCommunication;
		}	
	}
}
