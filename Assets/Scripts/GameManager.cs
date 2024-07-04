using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public struct LevelBounds
{
	public float Left, Top, Right, Bottom, Near, Far;
}

public enum GameState
{
	MainMenu, Playing, Paused, Waiting, Victory, GameOver
}

public class GameManager : MonoBehaviour, IGameStateListener
{
	#region Singleton
	private static GameManager _instance = null;
	public static GameManager Instance { get { return _instance; } }

	private void Awake()
	{
		if (_instance == null)
		{
			_instance = this;
			Init();
		}
		else
		{
			Destroy(gameObject);
		}
	}
	#endregion

	public LevelBounds Bounds = new LevelBounds();

	[SerializeField] private Transform _leftWall = null;
	[SerializeField] private Transform _rightWall = null;
	[SerializeField] private Transform _topWall = null;
	[SerializeField] private Transform _bottomWall = null;
	[SerializeField] private Transform _farWall = null;

	// Very basic FSM for tracking game state
	private System.Action<GameState, GameState> _OnGameStateChanged;
	private GameState _state = GameState.MainMenu;
	public GameState State {
		get { 
			return _state;
		}
		set	{
			_OnGameStateChanged?.Invoke(_state, value);
			_state = value;
		}
	}

	[Header("Player")]
	[SerializeField]
	private PlayerController playerPrefab = null;
	[SerializeField]
	private PlayerController playerController = null;
	
	[SerializeField]
	private Transform playerSpawn = null;

	public int Score { get; private set; }
	public int HiScore { get; private set; }

	private UIHandler _uiHandler = null;

	[Header("Level")]
	public int Level = 1;

	public LevelData LevelDatabase {  get; private set; }

	[Header("Audio")]
	[SerializeField]
	private AudioSource audioSource = null;
	[SerializeField]
	private float[] pitches = null;
	private int pitchIndex = 0;

	public System.Action OnPlayerRevival { get; set; }

	private void Init()
	{
		// Get the bounds and keep them ready
		Bounds.Left = _leftWall.position.x;
		Bounds.Right = _rightWall.position.x;
		Bounds.Top = _topWall.position.y;
		Bounds.Bottom = _bottomWall.position.y;
		Bounds.Near = playerSpawn.position.z - 1f;
		Bounds.Far = _farWall.position.z;

		_uiHandler = FindFirstObjectByType<UIHandler>();

		if (playerController == null)
		{
			playerController = Instantiate<PlayerController>(playerPrefab, playerSpawn.position, playerSpawn.rotation);
		}
		playerController.Init();

		LevelDatabase = Resources.Load<LevelData>("LevelData");
		HiScore = PlayerPrefs.GetInt("HiScore", 0);
	}

	public void AddScore(int score)
	{
		Score += score;
		bool isHiScore = Score > HiScore;
		if (isHiScore)
			HiScore = Score;

		_uiHandler.UpdateScore(Score, isHiScore);
	}

	public void StartGame()
	{
		State = GameState.Playing;
		EnemyManager.Instance.SpawnLevelEnemies(Level);
	}

	public void NextLevel()
	{ 
		// Do the cleanup and reset the level
		Level++;
		if (Level >= LevelDatabase.levels.Count)
		{
			// Win condition
			State = GameState.Victory;
			EndGame(true);
			return;
		}
		EnemyManager.Instance.ReInit();
	}

	public void RevivePlayer()
	{
		StartCoroutine(RevivalSequence());
	}

	IEnumerator RevivalSequence()
	{
		// Pause game
		State = GameState.Waiting;

		// Clear all projectiles
		OnPlayerRevival?.Invoke();

		yield return new WaitForSecondsRealtime(1f);

		// Replace player and play effect
		playerController.ReviveAtPosition(playerSpawn.position);

		// Update UI
		//_uiHandler.UpdateLives();

		// Resume playing
		State = GameState.Playing;
	}

	public void PlayBoom()
	{
		audioSource.pitch = pitches[pitchIndex++ % pitches.Length];
		audioSource.Play();
	}

	public void EndGame(bool didWin)
	{
		PlayerPrefs.SetInt("HiScore", Score);
		PlayerPrefs.Save();
	}

	public void BackToTitleScreen()
	{
		State = GameState.MainMenu;

		// Reset score
		Score = 0;
	}

	public void OnGameStateChanged(GameState fromState, GameState toState)
	{
		// Do all the state related handling here
	}
}
