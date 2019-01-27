using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

public class CollisionCheck : MonoBehaviour
{
	private List<Transform> PlayerTransforms;
	private List<Collider> PlayerColliders;
	private List<GameObject> Players;

	private Collider mCollider;

	public bool npc;
	public int loadedScene;
	
	void Start ()
	{
		mCollider = GetComponent<Collider>();
		
		PlayerTransforms = new List<Transform>();
		PlayerColliders = new List<Collider>();
		Players = new List<GameObject>();
	}
	
	void Update () {
		ReloadLists();
		for(int i=0;i<Players.Count;i++)
		{
			Vector3 direction;
			float distance;

			if (Players[i].GetHashCode() != gameObject.GetHashCode() &&
			    Physics.ComputePenetration(mCollider, transform.position, transform.rotation, 
				PlayerColliders[i], PlayerTransforms[i].position, PlayerTransforms[i].rotation, 
				out direction, out distance))
			{
				//Debug.Log(direction + " = " + distance);
				transform.position += direction.normalized * (distance+0.05f);
			}
		}
		
		if (npc && SocketIOScript.Instance.CurrentState == GameState.PLAY)
		{
			npc = false;
			StartCoroutine(moveAndWait());
		}
	}

	public void ReloadLists()
	{
		Players.Clear();
		PlayerTransforms.Clear();
		PlayerColliders.Clear();

		Players = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
		foreach (var player in Players)
		{
			PlayerTransforms.Add(player.transform);
			PlayerColliders.Add(player.GetComponent<Collider>());
		}
	}

	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.CompareTag("Finish"))
		{
			SocketIOScript.Instance.CurrentState = GameState.WIN;
			SocketIOScript.Instance.Winner = gameObject.name;
		}
	}

	IEnumerator moveAndWait()
	{
		float rand = UnityEngine.Random.Range(0.01f, 1f);
		if (rand < 0.2f)
		{
			transform.position += Vector3.right / 2;
		}
		else if (rand < 0.4f)
		{
			transform.position += Vector3.left / 2;				
		}
		else if (rand < 0.6f)
		{
			transform.position += Vector3.up / 2;
		}
		else if (rand < 0.8f)
		{
			transform.position += Vector3.down / 2;				
		}
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 1.5f));
		yield return StartCoroutine(moveAndWait());
	}
}
