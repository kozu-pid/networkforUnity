
// TCPで通信する場合は定義を有効にしてください.
#define USE_TRANSPORT_TCP

using UnityEngine;
using System.Collections;
using System.Net;


public class LibrarySample : MonoBehaviour {

	// 通信モジュール.
	public GameObject	transportTcpPrefab;
	public GameObject	transportUdpPrefab;

	// 通信用変数
#if USE_TRANSPORT_TCP
	TransportTCP		m_transport = null;
#else
	TransportUDP		m_transport = null;
#endif

	// 接続先のIPアドレス.
	private string		m_strings = "";

	// 接続先のポート番号.
	private const int 	m_port = 50765;

	private const int 	m_mtu = 1400;

	private bool 		isSelected = false;


	// Use this for initialization
	void Start ()
	{
		// Transportクラスのコンポーネントを取得.
#if USE_TRANSPORT_TCP
		GameObject obj = GameObject.Instantiate(transportTcpPrefab) as GameObject;
		m_transport = obj.GetComponent<TransportTCP>();
#else
		GameObject obj = GameObject.Instantiate(transportUdpPrefab) as GameObject;
		m_transport = obj.GetComponent<TransportUDP>();
#endif

		IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
		System.Net.IPAddress hostAddress = hostEntry.AddressList[0];
		Debug.Log(hostEntry.HostName);
		m_strings = hostAddress.ToString();
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (m_transport != null && m_transport.IsConnected() == true) {
			byte[] buffer = new byte[m_mtu];
			int recvSize = m_transport.Receive(ref buffer, buffer.Length);
			if (recvSize > 0) {
				string message = System.Text.Encoding.UTF8.GetString(buffer);
				Debug.Log(message);
			}
		}
	}

	void OnGUI()
	{
		if (isSelected == false) {
			OnGUISelectHost();
		}
		else {
			if (m_transport.IsServer() == true) {
				OnGUIServer();
			}
			else {
				OnGUIClient();
			}
		}
	}

	void OnGUISelectHost()
	{
#if USE_TRANSPORT_TCP
		if (GUI.Button (new Rect (20,40, 150,20), "Launch server.")) {
#else
		if (GUI.Button (new Rect (20,40, 150,20), "Launch Listener.")) {
#endif
			m_transport.StartServer(m_port, 1);
			isSelected = true;
		}
		
		// クライアントを選択した時の接続するサーバのアドレスを入力します.
		m_strings = GUI.TextField(new Rect(20, 100, 200, 20), m_strings);
#if USE_TRANSPORT_TCP
			if (GUI.Button (new Rect (20,70,150,20), "Connect to server")) {
#else
			if (GUI.Button (new Rect (20,70,150,20), "Connect to terminal")) {
#endif
			m_transport.Connect(m_strings, m_port);
			isSelected = true;
			m_strings = "";
		}	
	}

	void OnGUIServer()
	{
#if USE_TRANSPORT_TCP
		if (GUI.Button (new Rect (20,60, 150,20), "Stop server")) {
#else
		if (GUI.Button (new Rect (20,60, 150,20), "Stop Listener")) {
#endif
			m_transport.StopServer();
			isSelected = false;
			m_strings = "";
		}
	}


	void OnGUIClient()
	{
		// クライアントを選択した時の接続するサーバのアドレスを入力します.
		if (GUI.Button (new Rect (20,70,150,20), "Send message")) {
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes("Hellow, this is client.");	
			m_transport.Send(buffer, buffer.Length);
		}

		if (GUI.Button (new Rect (20,100, 150,20), "Disconnect")) {
			m_transport.Disconnect();
			isSelected = false;
			m_strings = "";
		}
	}

}
