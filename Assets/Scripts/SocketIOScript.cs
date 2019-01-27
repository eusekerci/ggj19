using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ChatData {
	public string id;
	public string msg;
};

public class ConData {
	public string id;
	public string count;
	public string msg;
};

public enum GameState
{
	PREPARE,
	PLAY,
	WIN,
}

public class SocketIOScript : MonoBehaviour
{
	private static SocketIOScript _instance;

	public static SocketIOScript Instance { get { return _instance; } }
	
	public Text AnnouncementText;
	public string ConnectionText;
	public string serverURL;

	public float movementSpeed;
	public Dictionary<int, Transform> Players;
	public List<KeyValuePair<int, string>> PlayerCommands;
	public List<KeyValuePair<int, string>> PlayerLoadCommands;
	public List<Transform> Dummies;
	public Transform PlayerPrefab;

	public GameState CurrentState;
	public string Winner;

	protected Socket socket;
	protected int _userCount;
	
	void OnDestroy() {
		DoClose ();
	}
	
	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(gameObject);
		} else {
			_instance = this;
		}
	}

	void Start () 
	{
		_userCount = 0;
		Winner = "";
		CurrentState = GameState.PREPARE;
		Players = new Dictionary<int, Transform>();
		PlayerCommands = new List<KeyValuePair<int, string>>();
		PlayerLoadCommands = new List<KeyValuePair<int, string>>();
		Dummies = new List<Transform>();
		DoOpen ();
	}

	void Update()
	{
		if(CurrentState == GameState.PREPARE)
		{
			AnnouncementText.text = _userCount + "\n" + ConnectionText + "\n\n\n"
				+ "Join in \n http;//192.168.3.68:31319";
			PlayerCommands.Clear();
			if (PlayerLoadCommands.Count > 0)
			{
				foreach (var loadCommand in PlayerLoadCommands)
				{
					if (loadCommand.Value == "Disconnected")
					{
						if(Players.ContainsKey(loadCommand.Key)) {
							GameObject go = Players[loadCommand.Key].gameObject;
							Destroy(go);
							Players.Remove(loadCommand.Key);
						}
					}
					else if (loadCommand.Value == "Connected")
					{
						GameObject go = Instantiate(PlayerPrefab.gameObject,
							new Vector3(Random.Range(-14.5f, 14.5f), Random.Range(5.1f, 12.1f), 0), Quaternion.identity);
						go.GetComponent<Renderer>().material.color = new Color(Random.Range(0.0f,1.0f), Random.Range(0.0f,1.0f), Random.Range(0.0f,1.0f));
						
						GameObject bosGo = Instantiate(PlayerPrefab.gameObject,
							new Vector3(Random.Range(-14.5f, 14.5f), Random.Range(5.1f, 12.1f), 0), Quaternion.identity);
						bosGo.GetComponent<Renderer>().material.color = new Color(Random.Range(0.0f,1.0f), Random.Range(0.0f,1.0f), Random.Range(0.0f,1.0f));
						bosGo.GetComponent<CollisionCheck>().npc = true;
						Dummies.Add(bosGo.transform);
						
						go.gameObject.name = "Player " + loadCommand.Key;
						Players.Add(loadCommand.Key, go.transform);
					}
				}
				PlayerLoadCommands.Clear();
			}
		}
		else if (CurrentState == GameState.PLAY)
		{
			AnnouncementText.text = "";
			PlayerLoadCommands.Clear();
			if (PlayerCommands.Count > 0) 
			{
				foreach (var command in PlayerCommands)
				{
					MovePlayer(command.Key, command.Value);
				}
				PlayerCommands.Clear();
			}
		}
		else if (CurrentState == GameState.WIN)
		{
			AnnouncementText.text = Winner + " WON!";
			UnloadAllScenesExcept("GameScene");
			if(Dummies.Count > 0)
			foreach (var dummy in Dummies)
			{
				Destroy(dummy.gameObject);
			}
			Dummies.Clear();
		}

		if (Input.GetKeyUp(KeyCode.A))
		{
			CurrentState = GameState.PREPARE;
		}
		else if (Input.GetKeyUp(KeyCode.Alpha1))
		{
			SceneManager.LoadScene(1, LoadSceneMode.Additive);
			CurrentState = GameState.PLAY;
		}
		else if (Input.GetKeyUp(KeyCode.Alpha2))
		{
			SceneManager.LoadScene(2, LoadSceneMode.Additive);
			CurrentState = GameState.PLAY;
		}
		else if (Input.GetKeyUp(KeyCode.Alpha3))
		{
			SceneManager.LoadScene(3, LoadSceneMode.Additive);
			CurrentState = GameState.PLAY;
		}
	}
	
	void DoOpen()
	{
		if (socket == null) {
			socket = IO.Socket (serverURL);
			
			socket.On("conpack", (data) =>
			{
				string str = data.ToString();
				ConData condata = JsonConvert.DeserializeObject<ConData>(str);
				
				//lock (PlayerLoadCommands)
				{
					_userCount = int.Parse(condata.count);
					PlayerLoadCommands.Add(new KeyValuePair<int, string>(int.Parse(condata.id), condata.msg));
				}
				//string strChatLog = "User #" + condata.id + " is " + condata.msg + " // Current count: " + condata.count;
				//Debug.Log(strChatLog);
			});
			socket.On ("chat", (data) => 
			{
				string str = data.ToString();
				ChatData chat = JsonConvert.DeserializeObject<ChatData> (str);

				PlayerCommands.Add(new KeyValuePair<int, string>(int.Parse(chat.id), chat.msg));
				//string strChatLog = "User #" + chat.id + ": " + chat.msg;
				//Debug.Log(strChatLog);
			});
		}
	}

	void DoClose() {
		if (socket != null) {
			socket.Disconnect ();
			socket = null;
		}
	}

	void MovePlayer(int id, string direction)
	{
		if (!Players.ContainsKey(id))
			return;
		if (direction == "Left")
		{
			Players[id].position -= new Vector3(movementSpeed, 0, 0);
		}
		else if (direction == "Right")
		{
			Players[id].position += new Vector3(movementSpeed, 0, 0);
		}
		else if (direction == "Down")
		{
			Players[id].position -= new Vector3(0, movementSpeed, 0);
		}
		else if (direction == "Up")
		{
			Players[id].position += new Vector3(0, movementSpeed, 0);
		}
	}
	
	void UnloadAllScenesExcept(string sceneName) 
	{
		int c = SceneManager.sceneCount;
		for (int i = 0; i < c; i++) 
		{
			Scene scene = SceneManager.GetSceneAt (i);
			if (scene.name != sceneName) 
			{
				SceneManager.UnloadSceneAsync (scene);
			}
		}
	}
}
