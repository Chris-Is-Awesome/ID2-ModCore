using HarmonyLib;

namespace ID2.ModCore;

[HarmonyPatch]
internal class Patches
{
	[HarmonyPostfix, HarmonyPatch(typeof(EntityEventsOwner), nameof(EntityEventsOwner.SendDetailedDeath))]
	public static void SendEntityDeathEvent(Entity ent, Killable.DetailedDeathData data)
	{
		// Don't run for PlayerEnt
		if (ent == Globals.Player)
			return;

		Events.EntityKilled(ent, data);
	}

	[HarmonyPostfix, HarmonyPatch(typeof(EntityEventsOwner), nameof(EntityEventsOwner.SendRoomChangeDone))]
	public static void SendRoomChangeEvent(LevelRoom to, LevelRoom from, EntityEventsOwner.RoomEventData data)
	{
		// Don't run event when rooms are null (from scene transition)
		if (to == null && from == null)
			return;

		Events.RoomChangeed(to, from, data);
	}

	[HarmonyPostfix, HarmonyPatch(typeof(ObjectUpdater.UpdateLayer), nameof(ObjectUpdater.UpdateLayer.SetPause))]
	public static void SendPauseEvent(bool pause)
	{
		Events.Paused(pause);
	}

	[HarmonyPostfix, HarmonyPatch(typeof(SceneDoor), nameof(SceneDoor.SaveStartPos))]
	public static void SpawnPositionUpdated(SceneDoor __instance, string wantedDoor)
	{
		Globals.SpawnPoint = string.IsNullOrEmpty(wantedDoor) ? __instance._correspondingDoor : wantedDoor;
	}
}