using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Phoenix;
using Newtonsoft.Json.Linq;

public struct PlayerInfo
{
    public string id;
    public string color;
    public float x;
    public float y;
}

public struct Controll
{
    public int x;
    public int y;

    public Controll(int _x, int _y) 
    {
        x = _x;
        y = _y;
    }
}

public struct PlayersList 
{
    public List<PlayerInfo> players;
}

public struct QueueMessage
{
    public string topic;
    public JObject payload;

    public QueueMessage(string _topic, JObject _payload) 
    {
        topic = _topic;
        payload = _payload;
    }
}

public class Connect : MonoBehaviour
{
    public Player playerPrefab;
    public Player enemyPrefab;
    private Queue<QueueMessage> _queue;
    private Queue<Controll> _controllQueue;
    private Channel _ch;
    private string _id;
    private Dictionary<string, Player> _players;

    public Button buttonLeft;
    public Button buttonRight;
    public Button buttonTop;
    public Button buttonDown;


    // Start is called before the first frame update
    void Start()
    {
        _queue = new Queue<QueueMessage>();
        _controllQueue = new Queue<Controll>();
        _players = new Dictionary<string, Player>();
        _id = GenerateRandomID();

        var factory = new WebsocketSharpFactory();
        var socket = new Socket(factory);

        socket.Connect("ws://localhost:4000/socket", new Dictionary<string, string>(){
            {"token", _id},
        });

        var channel = socket.MakeChannel("room:lobby");
        channel.On("on_connect", m => {
            _queue.Enqueue(new QueueMessage("on_connect", m.payload));
        });
        channel.On("player_joined", m => {
            _queue.Enqueue(new QueueMessage("player_joined", m.payload));
        });
        channel.On("player_leave", m => {
            _queue.Enqueue(new QueueMessage("player_leave", m.payload));      
        });
        channel.On("movement", m => {
            _queue.Enqueue(new QueueMessage("movement", m.payload));
        });

        channel.Join();
        _ch = channel;

        // Create button bindings 
        buttonLeft.onClick.AddListener(MoveLeft);
        buttonRight.onClick.AddListener(MoveRight);
        buttonTop.onClick.AddListener(MoveTop);
        buttonDown.onClick.AddListener(MoveDown);
    }

    // Update is called once per frame
    void Update()
    {
        while (_queue.Count > 0) {
            QueueMessage message = _queue.Dequeue();

            if (message.topic == "player_joined") 
            {
                PlayerInfo info = message.payload.ToObject<PlayerInfo>();
                CreatePlayer(info);
            }
            else if (message.topic == "on_connect")
            {
                PlayersList players = message.payload.ToObject<PlayersList>();

                foreach (PlayerInfo info in players.players) 
                {
                    CreatePlayer(info);
                }
            }
            else if (message.topic == "player_leave")
            {
                PlayerInfo info = message.payload.ToObject<PlayerInfo>();
                DestroyPlayer(info);                
            }
            else if (message.topic == "movement")
            {
                PlayerInfo info = message.payload.ToObject<PlayerInfo>();
                MovePlayer(info);
            }
        }

        while (_controllQueue.Count > 0) {
            Controll controll = _controllQueue.Dequeue();

            _ch.Push("controll", new Dictionary<string, object>(){
                {"x", controll.x},
                {"y", controll.y},
            });
        }
    }

    string GenerateRandomID()
    {
        return System.Guid.NewGuid().ToString();
    }

    void CreatePlayer(PlayerInfo info)
    {
        Player player;

        if (info.id == _id) 
        {
            player = Instantiate(playerPrefab, new Vector3(info.x, info.y), Quaternion.identity);
        }
        else 
        {
            player = Instantiate(enemyPrefab, new Vector3(info.x, info.y), Quaternion.identity);
        }

        Color color;
        ColorUtility.TryParseHtmlString(info.color, out color);
        player.GetComponent<SpriteRenderer>().color = color;

        _players.Add(info.id, player);

        Debug.Log($"creating player {info.id} at [{info.x}, {info.y}] with color {color}");
    }

    void MovePlayer(PlayerInfo info)
    {
        Player player = _players[info.id];
        player.transform.position = new Vector3(info.x, info.y);

        Debug.Log($"moving player {info.id} to position [{info.x}, {info.y}]");
    }

    void DestroyPlayer(PlayerInfo info)
    {
        Player player = _players[info.id];
        Destroy(player.gameObject);
        _players.Remove(info.id);

        Debug.Log($"removing player {info.id}");
    }

    void MoveLeft() 
    {
        EnqueueControll(-1, 0);
    }

    void MoveRight() 
    {
        EnqueueControll(1, 0);
    }

    void MoveTop()
    {
        EnqueueControll(0, 1);
    }

    void MoveDown()
    {
        EnqueueControll(0, -1);
    }

    void EnqueueControll(int x, int y) {
        _controllQueue.Enqueue(new Controll(x, y));
    }
}
