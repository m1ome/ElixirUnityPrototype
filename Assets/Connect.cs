using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Phoenix;

public class Connect : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var factory = new WebsocketSharpFactory();
        var socket = new Socket(factory);

        socket.Connect("ws://localhost:4000/socket", new Dictionary<string, string>(){
            {"token", "zalupoken"},
        });



        var channel = socket.MakeChannel("room:lobby");
        channel.On("on_connect", m => {
            Debug.Log($"on_connect: {m}");
        });
        channel.On("player_joined", m => {
           Debug.Log($"player_joined: {m}");
        });

        channel.Join();

        // Send three movement
        channel.Push("controll", new Dictionary<string, object>(){
            {"x", 1},
            {"y", 1},
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
