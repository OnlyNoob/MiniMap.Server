﻿#if CLIENT
using static GarrysModLuaShared.Lua;

namespace GarrysModLuaShared
{
    /// <summary>
    ///     This library is used internally by Garry's Mod to help keep track of achievement progress and unlock the
    ///     appropriate achievements once a certain number is reached.
    ///     <para />
    ///     However, this library can also be used by anyone else to forcefully unlock certain achievements.
    /// </summary>
    static class achievements
    {
        /// <summary>Tells the client that balloon has popped. This counts towards the Balloon Popper achievement.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        public static void BalloonPopped(LuaState luaState)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(BalloonPopped));
                lua_pcall(luaState);
            }
        }

        /// <summary>Returns the amount of achievements in Garry's Mod.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        /// <returns>The amount of achievements available.</returns>
        public static double Count(LuaState luaState)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(Count));
                lua_pcall(luaState, 0, 1);
                return lua_tonumber(luaState);
            }
        }

        /// <summary>Adds one to the count of balls eaten. Once this count reaches 200, the 'Ball Eater' achievement is unlocked.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        public static void EatBall(LuaState luaState)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(EatBall));
                lua_pcall(luaState);
            }
        }

        /// <summary>Retrieves progress of the given achievement.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        /// <param name="achievementId">The ID of achievement to retrieve progress of. Note: IDs start from 0, not 1.</param>
        /// <returns>Progress of the given achievement.</returns>
        public static double GetCount(LuaState luaState, double achievementId)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(GetCount));
                lua_pushnumber(luaState, achievementId);
                lua_pcall(luaState, 1, 1);
                return lua_tonumber(luaState);
            }
        }

        /// <summary>Retrieves description of the given achievement.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        /// <param name="achievementId">The ID of achievement to retrieve description of. Note: IDs start from 0, not 1.</param>
        /// <returns>Description of the given achievement.</returns>
        public static string GetDesc(LuaState luaState, double achievementId)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(GetDesc));
                lua_pushnumber(luaState, achievementId);
                lua_pcall(luaState, 1, 1);
                return ToManagedString(luaState);
            }
        }

        /// <summary>Retrieves goal of the given achievement.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        /// <param name="achievementId">The ID of achievement to retrieve goal of. Note: IDs start from 0, not 1.</param>
        /// <returns>Goal of the given achievement.</returns>
        public static double GetGoal(LuaState luaState, double achievementId)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(GetGoal));
                lua_pushnumber(luaState, achievementId);
                lua_pcall(luaState, 1, 1);
                return lua_tonumber(luaState);
            }
        }

        /// <summary>Retrieves name of the given achievement.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        /// <param name="achievementId">The ID of achievement to retrieve name of. Note: IDs start from 0, not 1.</param>
        /// <returns>Name of the given achievement.</returns>
        public static string GetName(LuaState luaState, double achievementId)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(GetName));
                lua_pushnumber(luaState, achievementId);
                lua_pcall(luaState, 1, 1);
                return ToManagedString(luaState);
            }
        }

        /// <summary>Increases "War Zone" achievement progress by 1.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        public static void IncBaddies(LuaState luaState)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(IncBaddies));
                lua_pcall(luaState);
            }
        }

        /// <summary>Increases "Innocent Bystander" achievement progress by 1.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        public static void IncBystander(LuaState luaState)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(IncBystander));
                lua_pcall(luaState);
            }
        }

        /// <summary>Increases "Bad Friend" achievement progress by 1.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        public static void IncGoodies(LuaState luaState)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(IncGoodies));
                lua_pcall(luaState);
            }
        }

        /// <summary>Used in GMod 12 in the achievements menu to show the user if they have unlocked certain achievements.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        /// <param name="achievementId">Internal Achievement ID number.</param>
        /// <returns>Returns true if the given <paramref name="achievementId" /> is achieved; otherwise false.</returns>
        public static bool IsAchieved(LuaState luaState, double achievementId)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(IsAchieved));
                lua_pushnumber(luaState, achievementId);
                lua_pcall(luaState, 1, 1);
                return lua_toboolean(luaState) == 1;
            }
        }

        /// <summary>This function is used by the engine to track achievement progress for 'Destroyer'.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        public static void Remover(LuaState luaState)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(Remover));
                lua_pcall(luaState);
            }
        }

        /// <summary>Tracks achievement progress for 'Procreator'.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        public static void SpawnedNPC(LuaState luaState)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(SpawnedNPC));
                lua_pcall(luaState);
            }
        }

        /// <summary>Tells the client that a prop has been spawned by the user, used to track achievement progress for 'Creator'.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        public static void SpawnedProp(LuaState luaState)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(SpawnedProp));
                lua_pcall(luaState);
            }
        }

        /// <summary>Tells the client that a ragdoll has been spawned. Used to track achievement progress for 'Dollhouse'.</summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        public static void SpawnedRagdoll(LuaState luaState)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(SpawnedRagdoll));
                lua_pcall(luaState);
            }
        }

        /// <summary>
        ///     An automatically called function that adds one to a count of how many times the spawnmenu has been opened.
        ///     It's purpose is to track progress of 'Menu User' achievement.
        /// </summary>
        /// <param name="luaState">Pointer to lua_State struct.</param>
        public static void SpawnMenuOpen(LuaState luaState)
        {
            lock (SyncRoot)
            {
                lua_getglobal(luaState, nameof(achievements));
                lua_getfield(luaState, -1, nameof(SpawnMenuOpen));
                lua_pcall(luaState);
            }
        }
    }
}

#endif