using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;

public class SocketSampleUDP : MonoBehaviour
{
	
	// 接続先のIPアドレス.
	private string			m_address = "";
	
	// 接続先のポート番号.
	private const int 		m_port = 50765;

	// 通信用変数
	private Socket			m_socket = null;

	// 状態.
	private State			m_state;

	// 状態定義
	private enum State
	{
		SelectHost = 0,
		CreateListener,
		ReceiveMessage,
		CloseListener,
		SendMessage,
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
		case State.CreateListener:
			CreateListener();
			break;

		case State.ReceiveMessage:
			ReceiveMessage();
			break;

		case State.CloseListener:
			CloseListener();
			break;

		case State.SendMessage:
			SendMessage();
			break;

		default:
			break;
		}
	}

	// 他の端末からのメッセージ受信.
	void CreateListener()
	{
		Debug.Log("[UDP]Start communication.");
		
		// ソケットを生成します.
		m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		// 使用するポート番号を割り当てます.
		m_socket.Bind(new IPEndPoint(IPAddress.Any, m_port));

		m_state = State.ReceiveMessage;
	}

	// 他の端末からのメッセージ受信.
	void ReceiveMessage()
	{
		byte[] buffer = new byte[1400];
		IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
		EndPoint senderRemote = (EndPoint)sender;

		if (m_socket.Poll(0, SelectMode.SelectRead)) {
			int recvSize = m_socket.ReceiveFrom(buffer, SocketFlags.None, ref senderRemote);
			if (recvSize > 0) {
				string message = System.Text.Encoding.UTF8.GetString(buffer);
				Debug.Log(message);
				m_state = State.CloseListener;
			}
		}
	}
	
	// 待ち受け終了.
	void CloseListener()
	{	
		// 待ち受けを終了します.
		if (m_socket != null) {
			m_socket.Close();
			m_socket = null;
		}

		m_state = State.Endcommunication;

		Debug.Log("[UDP]End communication.");
	}

	// クライアントとの接続, 送信, 切断.
	void SendMessage()
	{
		Debug.Log("[UDP]Start communication.");

		// サーバへ接続.
		m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		// メッセージ送信.
		byte[] buffer = System.Text.Encoding.UTF8.GetBytes("Hello, this is client.");
		IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(m_address), m_port);
		m_socket.SendTo(buffer, buffer.Length, SocketFlags.None, endpoint);

		// 切断.
		m_socket.Shutdown(SocketShutdown.Both);
		m_socket.Close();

		m_state = State.Endcommunication;

		Debug.Log("[UDP]End communication.");
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
			m_state = State.CreateListener;
		}
		
		// クライアントを選択した時の接続するサーバのアドレスを入力します.
		m_address = GUI.TextField(new Rect(20, 100, 200, 20), m_address);
		if (GUI.Button (new Rect (20,70,150,20), "Connect to server")) {
			m_state = State.SendMessage;
		}	
	}
}
