using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;



public class TransportTCP : MonoBehaviour {

	//
	// ソケット接続関連.
	//

	// リスニングソケット.
	private Socket			m_listener = null;

	// クライアントとの接続用ソケット.
	private Socket			m_socket = null;

	// 送信バッファ.
	private PacketQueue		m_sendQueue;
	
	// 受信バッファ.
	private PacketQueue		m_recvQueue;
	
	// サーバフラグ.	
	private bool	 		m_isServer = false;

	// 接続フラグ.
	private	bool			m_isConnected = false;

	//
	// イベント関連のメンバ変数.
	//

	// イベント通知のデリゲート.
	public delegate void 	EventHandler(NetEventState state);

	private EventHandler	m_handler;

	//
	// スレッド関連のメンバ変数.
	//

	// スレッド実行フラグ.
	protected bool			m_threadLoop = false;
	
	protected Thread		m_thread = null;

	private static int 		s_mtu = 1400;


	// Use this for initialization
	void Start ()
    {

        // 送受信バッファを作成します.
        m_sendQueue = new PacketQueue();
        m_recvQueue = new PacketQueue();	
	}
	
	// Update is called once per frame
	void Update ()
    {
	}

	// 待ち受け開始.
	public bool StartServer(int port, int connectionNum)
	{
        Debug.Log("StartServer called.!");

        // リスニングソケットを生成します.
        try {
			// ソケットを生成します.
			m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			// 使用するポート番号を割り当てます.
			m_listener.Bind(new IPEndPoint(IPAddress.Any, port));
			// 待ち受けを開始します.
			m_listener.Listen(connectionNum);
        }
        catch {
			Debug.Log("StartServer fail");
            return false;
        }

        m_isServer = true;

        return LaunchThread();
    }

	// 待ち受け終了.
    public void StopServer()
    {
		m_threadLoop = false;
        if (m_thread != null) {
            m_thread.Join();
            m_thread = null;
        }

        Disconnect();

        if (m_listener != null) {
            m_listener.Close();
            m_listener = null;
        }

        m_isServer = false;

        Debug.Log("Server stopped.");
    }


    // 接続.
    public bool Connect(string address, int port)
    {
        Debug.Log("TransportTCP connect called.");

        if (m_listener != null) {
            return false;
        }

		bool ret = false;
        try {
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_socket.NoDelay = true;
            m_socket.SendBufferSize = 0;
            m_socket.Connect(address, port);
			ret = LaunchThread();
		}
        catch {
            m_socket = null;
        }

		if (ret == true) {
			m_isConnected = true;
			Debug.Log("Connection success.");
		}
		else {
			m_isConnected = false;
			Debug.Log("Connect fail");
		}

        if (m_handler != null) {
            // 接続結果を通知します.
			NetEventState state = new NetEventState();
			state.type = NetEventType.Connect;
			state.result = (m_isConnected == true) ? NetEventResult.Success : NetEventResult.Failure;
            m_handler(state);
			Debug.Log("event handler called");
        }

        return m_isConnected;
    }

	// 切断.
    public void Disconnect() {
        m_isConnected = false;

        if (m_socket != null) {
            // ソケットのクローズ.
            m_socket.Shutdown(SocketShutdown.Both);
            m_socket.Close();
            m_socket = null;
        }

        // 切断を通知します.
        if (m_handler != null) {
			NetEventState state = new NetEventState();
			state.type = NetEventType.Disconnect;
			state.result = NetEventResult.Success;
			m_handler(state);
        }
    }

    // 送信処理.
    public int Send(byte[] data, int size)
	{
		if (m_sendQueue == null) {
			return 0;
		}

        return m_sendQueue.Enqueue(data, size);
    }

    // 受信処理.
    public int Receive(ref byte[] buffer, int size)
	{
		if (m_recvQueue == null) {
			return 0;
		}

        return m_recvQueue.Dequeue(ref buffer, size);
    }

	// イベント通知関数登録.
    public void RegisterEventHandler(EventHandler handler)
    {
        m_handler += handler;
    }

	// イベント通知関数削除.
    public void UnregisterEventHandler(EventHandler handler)
    {
        m_handler -= handler;
    }

	// スレッド起動関数.
	bool LaunchThread()
	{
		try {
			// Dispatch用のスレッド起動.
			m_threadLoop = true;
			m_thread = new Thread(new ThreadStart(Dispatch));
			m_thread.Start();
		}
		catch {
			Debug.Log("Cannot launch thread.");
			return false;
		}
		
		return true;
	}

	// スレッド側の送受信処理.
    public void Dispatch()
	{
		Debug.Log("Dispatch thread started.");

		while (m_threadLoop) {
			// クライアントからの接続を待ちます.
			AcceptClient();

			// クライアントとの小受信を処理します.
			if (m_socket != null && m_isConnected == true) {

	            // 送信処理.
	            DispatchSend();

	            // 受信処理.
	            DispatchReceive();
	        }

			Thread.Sleep(5);
		}

		Debug.Log("Dispatch thread ended.");
    }

	// クライアントとの接続.
	void AcceptClient()
	{
		if (m_listener != null && m_listener.Poll(0, SelectMode.SelectRead)) {
			// クライアントから接続されました.
			m_socket = m_listener.Accept();
			m_isConnected = true;
			Debug.Log("Connected from client.");
		}
	}

	// スレッド側の送信処理.
    void DispatchSend()
	{
        try {
            // 送信処理.
            if (m_socket.Poll(0, SelectMode.SelectWrite)) {
				byte[] buffer = new byte[s_mtu];

                int sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
                while (sendSize > 0) {
                    m_socket.Send(buffer, sendSize, SocketFlags.None);
                    sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
                }
            }
        }
        catch {
            return;
        }
    }

	// スレッド側の受信処理.
    void DispatchReceive()
	{
        // 受信処理.
        try {
            while (m_socket.Poll(0, SelectMode.SelectRead)) {
				byte[] buffer = new byte[s_mtu];

                int recvSize = m_socket.Receive(buffer, buffer.Length, SocketFlags.None);
                if (recvSize == 0) {
                    // 切断.
                    Debug.Log("Disconnect recv from client.");
                    Disconnect();
                }
                else if (recvSize > 0) {
                    m_recvQueue.Enqueue(buffer, recvSize);
                }
            }
        }
        catch {
            return;
        }
    }

	// サーバか確認.
	public bool IsServer() {
		return m_isServer;
	}
	
    // 接続確認.
    public bool IsConnected() {
        return m_isConnected;
    }

}
