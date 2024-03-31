namespace Wozware.Downslope
{
	public enum SceneLoaderStates
	{
		Title,
		Loading,
		Idle,
	}

	public enum FadeOverlayStates
	{
		FadingIn,
		FadingOut,
		FadedIn,
		FadedOut,
	}

	public enum GameStates
	{
		Init,
		Loading,
		LevelCreator,
		Menu,
		Map,
		Game,
		Paused,
	}

	public enum GameModes
	{
		Tutorial,
		Challenge,
		Arcade,
	}

	public enum PlayerStates
	{
		Hidden,
		Stopped,
		Moving,
		Airborne,
		Stunned,
	}

	public enum UIViewTypes
	{
		MainMenu,
		Map,
		LevelCreator,
		Game,
	}

	public enum MapTypes
	{
		Free,
		Challenger,
	}

	public enum MapPointTypes
	{
		Official,
		Custom,
		Town,
	}

	public enum RanWeightedLayerTypes
	{
		Obstacle,
		ObstacleIce,
		IcePath,
		SnowVariation,
		WorldEdge,
		XOffset,
		IcePathSpawn,
	}

	public enum SlopeAngleTypes
	{
		GradualSlope, // 8 deg
		CasualSlope, // 15 deg
		StandardSlope, // 25 deg
		SteepSlope, // 45 deg
		AlpineSlope, // 65 deg
	}

	public enum TerrainTypes
	{
		Powder,
		Ice,
		Air,
	}

	public enum TrailTypes
	{
		Straight,
		Carve,
		AfterCarve,
	}

	public enum CollisionTypes
	{
		None,
		Powder,
		Shrub,
		SmallTree,
		LargeTree,
		SmallRamp,
		Stump,
		LargeRamp,
	}

	public enum ChunkFlagTypes
	{
		Empty,
		IcePath,
		Obstacle,
	}

	public enum LevelCreatorStates
	{
		None,
		Place,
		Erase,
		Info,
		Move,
		Save,
		Load,
	}

	public enum LevelCreatorPlacementTypes
	{
		Place,
		Erase,
		Move,
	}

	public enum LevelCreatorObjectTypes
	{
		Sprite,
		Eraser,
		StartPoint,
		EndPoint,
	}

	public enum SpriteLayerTypes
	{
		Ground,
		GroundPlus,
		Obstacle,
		Trail,
		Sky,
		Level,
	}

	public enum RebindableControlTypes
	{
		Game_Enter,
		Game_Escape,
		Player_Carve_Left,
		Player_Carve_Right,
		Player_Brake,
		Player_SendIt,
		LC_Scroll,
		LC_Place,
		LC_Shift,
		LC_Ctrl,
	}

}