using System;
using System.Text;
/*
namespace Lua
{
	public sealed class Event
	{
		static luaL_Key sendkey = new luaL_Key();
		static luaL_Key skipKey = new luaL_Key();
		static luaL_Keys idkeys;
		static int idstart;
		static Event()
		{
			Array values = Enum.GetValues(typeof(ID));
			if (values.Length > 0)
			{
				int minValue = int.MaxValue;
				int maxValue = int.MinValue;
				for (int i = 0; i < values.Length; ++i)
				{
					int value = (int)values.GetValue(i);
					if (minValue > value)
						minValue = value;
					if (maxValue < value)
						maxValue = value;
				}
				idstart = minValue;
				idkeys = new luaL_Keys(maxValue - minValue + 1);
			}
		}

		const string EVENT_PROGRAM = @"
local skip = {...}
skip = skip[1]

local stacktrace = stacktrace
local create
local resume

local targetlist = {}
local globallist = {}
local globalinvalid = {}
local globalpri = {}
local globalsort = {}

local mt = {__mode = 'k'}
setmetatable(targetlist, mt)

local function asserttrace(co, result, message, ...)
	if not result then
		error(stacktrace(co, message or 'assertion failed!'), 2)
	end
	return select(1, message, ...)
end

local function run(func, ...)
	if not create and not resume then
		create = coroutine.create
		resume = coroutine.resume
	end
	local co = create(func)
	return asserttrace(co, resume(co, ...))
end

local function targetsend(target, event, ...)
	local events = targetlist[target]
	if not events then
		return false
	end
	local list = events[event]
	if not list then
		return false
	end
	for i = #list, 1, -1 do
		if run(list[i], target, event, ...) == skip then
			return true
		end
	end
	return false
end

local function targetbind(target, event, func)
	local events = targetlist[target] or {}
	targetlist[target] = events
	local list = events[event] or {}
	events[event] = list
	table.insert(list, func)
end

local function globalsend(event, ...)
	local funcs = globallist[event]
	if funcs then
		if globalinvalid[event] then
			globalinvalid[event] = nil
			local sort = globalsort[event]
			if not sort then
				local pris = globalpri[event]
				sort = function(a, b)
					local prib = pris[b]
					if not prib then
						return true
					end
					local pria = pris[a]
					if not pria then
						return false
					end
					return pria > prib
				end
				globalsort[event] = sort
			end
			table.sort(funcs, sort)
		end
		for _, func in ipairs(funcs) do
			if run(func, event, ...) == skip then
				return true
			end
		end
	end
	return false
end

local function globalbind(event, func, pri)
	local list = globallist[event]
	local pris = globalpri[event]
	if not list then
		list = {}
		globallist[event] = list
		pris = {}
		globalpri[event] = pris
	end
	if not pris[func] then
		table.insert(list, func)
	end
	pris[func] = pri or 0
	globalinvalid[event] = true
end

event = {}
event.skip = skip

function event.send(first, ...)
	if type(first) == 'string' then
		return globalsend(first, ...)
	else
		return targetsend(first, ...)
	end
end

function event.bind(first, ...)
	if type(first) == 'string' then
		return globalbind(first, ...)
	else
		return targetbind(first, ...)
	end
end

return event.send
";

		void Init(State state)
		{
			IntPtr L = state;
			if (API.luaL_loadbuffer(L, Encoding.UTF8.GetBytes(EVENT_PROGRAM), "@event") != 0)
			{
				Log.Error(API.lua_tostring(L, -1));
				API.lua_pop(L, 1);
			}
			else
			{
				API.lua_pushlightuserdata(L, skipKey);
				if (API.lua_pcall(L, 1, 1, 0) != 0)
				{
					Log.Error(API.lua_tostring(L, -1));
					API.lua_pop(L, 1);
				}
				else
				{
					API.lua_pushlightuserdata(L, sendkey);
					API.lua_insert(L, -2);
					API.lua_settable(L, Consts.LUA_REGISTRYINDEX);
				}
			}
			if (idkeys != null)
			{
				string[] names = Enum.GetNames(typeof(ID));
				for (int i = 0; i < names.Length; ++i)
				{
					int value = (int)(ID)Enum.Parse(typeof(ID), names[i], false);
					API.lua_pushlightuserdata(L, idkeys[value - idstart]);
					API.lua_pushstring(L, names[i]);
					API.lua_settable(L, Consts.LUA_REGISTRYINDEX);
				}
			}
		}
		public enum ID
		{
			OnTouchUp,
			OnTouchDown,
			OnTouchMove,
			OnTouchInto,
			OnTouchCancel,

			OnLoad,
			OnShow,
			OnHide,
			OnUpdate,

			OnClick,
			OnText,
		}
		private class Sender
		{
			IntPtr L;
			int top;
			bool ready;
			public Sender(State state)
			{
				L = state;
				top = -1;
				ready = false;
			}
			private void ResetL()
			{
				if (top == -1)
					top = API.lua_gettop(L);
				else
					API.lua_settop(L, top);
				API.lua_pushlightuserdata(L, sendkey);
				API.lua_gettable(L, Consts.LUA_REGISTRYINDEX);
				ready = true;
			}
			public void Reset(string evt)
			{
				ResetL();
				API.lua_pushstring(L, evt);
			}
			public void Reset(ID evt)
			{
				ResetL();
				API.lua_pushlightuserdata(L, idkeys[(int)evt - idstart]);
				API.lua_gettable(L, Consts.LUA_REGISTRYINDEX);
			}
			public void Reset<T>(T t, string evt)
			{
				ResetL();
				API.lua_pushtype<T>(L, t);
				API.lua_pushstring(L, evt);
			}
			public void Reset<T>(T t, ID evt)
			{
				ResetL();
				API.lua_pushtype<T>(L, t);
				API.lua_pushlightuserdata(L, idkeys[(int)evt - idstart]);
				API.lua_gettable(L, Consts.LUA_REGISTRYINDEX);
			}

			bool Dispatch()
			{
				if (!ready)
					return false;
				ready = false;
				int oldtop = top;
				top = -1;
				if (API.lua_pcall(L, API.lua_gettop(L) - oldtop - 1, 0, 0) != 0)
				{
					string error = API.lua_tostring(L, -1);
					API.lua_pop(L, 1);
#if ASSERT
					throw new Error(error);
#else
					Log.Error(error);
#endif
					return false;
				}
				return true;
			}
		}
	}
}*/