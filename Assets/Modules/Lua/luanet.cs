/*using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Lua
{
    public abstract class Table
    {
        public abstract bool Push(IntPtr L);
    }

    public abstract class Thread
    {
        public abstract bool Push(IntPtr L);
        public abstract object[] Call(int nresults, params object[] args);
        public object[] Call(params object[] args)
        {
            return Call(Consts.LUA_MULTRET, args);
        }
    }

    public abstract class MetaType
    {
        protected virtual void InitConst(State2 state, int typetable) { }
        protected virtual bool HasNew() { return false; }
        protected virtual bool HasMethod() { return false; }
        protected virtual bool HasGet() { return false; }
        protected virtual bool HasSet() { return false; }
        protected virtual bool HasGetStatic() { return false; }
        protected virtual bool HasSetStatic() { return false; }
        protected virtual bool HasGetIndex() { return false; }
        protected virtual bool HasSetIndex() { return false; }
        protected virtual bool GetIndex(State2 state) { return false; }
        protected virtual bool SetIndex(State2 state) { return false; }

        protected virtual bool GetNewFunction(State2 state) { return false; }
        protected virtual bool GetMethodFunction(State2 state, string name) { return false; }
        protected virtual bool GetGetFunction(State2 state, string name) { return false; }
        protected virtual bool GetSetFunction(State2 state, string name) { return false; }
        protected virtual bool GetGetStaticFunction(State2 state, string name) { return false; }
        protected virtual bool GetSetStaticFunction(State2 state, string name) { return false; }
        protected virtual bool GetAddFunction(State2 state) { return false; }
        protected virtual bool GetSubFunction(State2 state) { return false; }
        protected virtual bool GetMulFunction(State2 state) { return false; }
        protected virtual bool GetDivFunction(State2 state) { return false; }
        protected virtual bool GetModFunction(State2 state) { return false; }
        protected virtual bool GetUnmFunction(State2 state) { return false; }
        protected virtual bool GetEqualFunction(State2 state) { return false; }
        protected virtual bool GetLessThanFunction(State2 state) { return false; }
        protected virtual bool GetLessEqualFunction(State2 state) { return false; }
        protected virtual bool GetCallFunction(State2 state) { return false; }

        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __type(IntPtr L)
        {
            State2 s = L;
            object o = null;
            if (s.ToType(1, ref o))
            {
                Type type = o.GetType();
                API.lua_pushliteral(L, Utils.NameOf(type));
            }
            else
            {
                API.lua_pushliteral(L, API.lua_typename(L, Consts.LUA_TUSERDATA));
            }
            return 1;
        }
        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __tostring(IntPtr L)
        {
            State2 s = L;
            object o = null;
            if (s.ToType(1, ref o))
            {
                API.lua_pushliteral(L, o.ToString());
            }
            else
            {
                API.lua_pushliteral(L, string.Format("unknown type:{0}", API.lua_topointer(L, 1).ToString("x8")));
            }
            return 1;
        }
        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __gc(IntPtr L)
        {
            State2 s = L;
            IntPtr ptr = API.lua_touserdata(L, 1);
            GCHandle gch = (GCHandle)Marshal.ReadIntPtr(ptr);
            s.FreeRef(gch.Target);
            gch.Free();
            return 0;
        }

        private static luaL_Key metakey = null;
        private static MetaType from(IntPtr L, int idx)
        {
            IntPtr ptr = API.lua_touserdata(L, idx);
            if (ptr == IntPtr.Zero)
                return null;
            GCHandle gch = (GCHandle)Marshal.ReadIntPtr(ptr);
            return gch.Target as MetaType;
        }
        private static void into(IntPtr L, MetaType metatype)
        {
            if (metakey == null)
            {
                metakey = new luaL_Key();
                API.lua_pushlightuserdata(L, metakey);
                API.lua_newtable(L);
                API.lua_pushliteral(L, "__gc");
                API.lua_pushcfunction(L, __gc);
                API.lua_settable(L, -3);
                API.lua_settable(L, Consts.LUA_REGISTRYINDEX);
            }
            if (metatype == null)
            {
                API.lua_pushnil(L);
            }
            else
            {
                GCHandle gch = GCHandle.Alloc(metatype);
                IntPtr ptr = API.lua_newuserdata(L, Marshal.SizeOf(typeof(IntPtr)));
                Marshal.WriteIntPtr(ptr, (IntPtr)gch);
                API.lua_pushlightuserdata(L, metakey);
                API.lua_gettable(L, Consts.LUA_REGISTRYINDEX);
                API.lua_setmetatable(L, -2);
            }
        }
        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __method(IntPtr L)
        {
            State2 state = L;
            string name = null;
            MetaType metatype = from(L, API.lua_upvalueindex(1));
            if (metatype == null || !state.ToType(1, ref name))
            {
                return 0;
            }
            if (!metatype.GetMethodFunction(state, name))
            {
                return 0;
            }
            return 1;
        }
        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __get(IntPtr L)
        {
            State2 state = L;
            string name = null;
            MetaType metatype = from(L, API.lua_upvalueindex(1));
            if (metatype == null || !state.ToType(1, ref name))
            {
                return 0;
            }
            if (!metatype.GetGetFunction(state, name))
            {
                return 0;
            }
            return 1;
        }
        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __set(IntPtr L)
        {
            State2 state = L;
            string name = null;
            MetaType metatype = from(L, API.lua_upvalueindex(1));
            if (metatype == null || !state.ToType(1, ref name))
            {
                return 0;
            }
            if (!metatype.GetSetFunction(state, name))
            {
                return 0;
            }
            return 1;
        }
        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __gets(IntPtr L)
        {
            State2 state = L;
            string name = null;
            MetaType metatype = from(L, API.lua_upvalueindex(1));
            if (metatype == null || !state.ToType(1, ref name))
            {
                return 0;
            }
            if (!metatype.GetGetStaticFunction(state, name))
            {
                return 0;
            }
            return 1;
        }
        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __sets(IntPtr L)
        {
            State2 state = L;
            string name = null;
            MetaType metatype = from(L, API.lua_upvalueindex(1));
            if (metatype == null || !state.ToType(1, ref name))
            {
                return 0;
            }
            if (!metatype.GetSetStaticFunction(state, name))
            {
                return 0;
            }
            return 1;
        }
        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __index(IntPtr L)
        {
            State2 state = L;
            MetaType metatype = from(L, API.lua_upvalueindex(1));
            if (metatype == null)
            {
                return 0;
            }
            if (!metatype.GetIndex(state))
            {
                return 0;
            }
            API.lua_pushboolean(L, 1);
            API.lua_insert(L, -2);
            return 2;
        }
        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __newindex(IntPtr L)
        {
            State2 state = L;
            MetaType metatype = from(L, API.lua_upvalueindex(1));
            if (metatype == null)
            {
                return 0;
            }
            if (!metatype.SetIndex(state))
            {
                return 0;
            }
            API.lua_pushboolean(L, 1);
            return 1;
        }

        static Function genindex;
        static Function gennewindex;
        static Function genindexs;
        static Function gennewindexs;
        static bool firstInited = false;
        private static void firstInit(State2 state, string name)
        {
            if (firstInited)
                return;
            firstInited = true;

            const string index = @"
function(type, cache, method, get, indexer, gets)
    local rawset = rawset
    local rawget = rawget
    local typeof = typeof
    local unpack = unpack
    return function(o, name)
        local meta
        meta = rawget(type, name)
        if meta then
            return meta
        end
        meta = cache[name]
        if meta then
            return meta(o, name)
        end
        meta = method & method(name)
        if meta then
            rawset(type, name, meta)
            return meta
        end
        meta = get & get(name)
        if meta then
            cache[name] = meta
            return meta(o, name)
        end
        if indexer then
            local result
            if typeof(name) == 'table' then
                result, meta = indexer(unpack(name))
            else
                result, meta = indexer(name)
            end
            if result then
                return meta
            end
        end
        return gets & gets(name)
    end
end
";
            const string newindex = @"
function(type, cache, set, indexer, sets)
    local rawset = rawset
    local rawget = rawget
    local typeof = typeof
    local unpack = unpack
    local insert = table.insert
    return function(o, name, value)
        local meta
        meta = cache[name]
        if meta then
            return meta(o, name, value)
        end
        meta = set & set(name)
        if meta then
            cache[name] = meta
            return meta(o, name, value)
        end
        if indexer then
            local result
            if typeof(name) == 'table' then
                insert(name, value)
                result = indexer(unpack(name))
            else
                result = indexer(name, value)
            end
            if result then
                return
            end
        end
        return sets & sets(name, value)
    end
end
";
            const string indexs = @"
function(type, cache, method, gets)
    local rawset = rawset
    local rawget = rawget
    return function(name)
        local meta
        meta = rawget(type, name)
        if meta then
            return meta
        end
        meta = cache[name]
        if meta then
            return meta(name)
        end
        meta = method & method(name)
        if meta then
            rawset(type, name, meta)
            return meta
        end
        meta = gets & gets(name)
        if meta then
            cache[name] = meta
            return meta(name)
        end
    end
end
";
            const string newindexs = @"
function(type, cache, sets)
    local rawset = rawset
    local rawget = rawget
    return function(name, value)
        local meta
        meta = cache[name]
        if meta then
            return meta(name, value)
        end
        meta = sets & sets(name)
        if meta then
            cache[name] = meta
            return meta(name, value)
        end
        rawset(type, name, value)
    end
end
";
            const string compilestr = "return " + index + ", " + newindex + ", " + indexs + ", " + newindexs;
            object[] funs = state.Compile(compilestr, "@meta").Call();
            genindex = (Function)funs[0];
            gennewindex = (Function)funs[1];
            genindexs = (Function)funs[2];
            gennewindexs = (Function)funs[3];
        }

        public void Init(State2 state, Type type, string name)
        {
            int typetable = API.lua_gettop(state);
            int metatable = typetable - 1;
            if (HasGet())
                API.lua_newtable(state);
            else
                API.lua_pushnil(state);
            int cacheget = API.lua_gettop(state);
            if (HasSet())
                API.lua_newtable(state);
            else
                API.lua_pushnil(state);
            int cacheset = API.lua_gettop(state);
            if (HasGetStatic())
                API.lua_newtable(state);
            else
                API.lua_pushnil(state);
            int cachegets = API.lua_gettop(state);
            if (HasSetStatic())
                API.lua_newtable(state);
            else
                API.lua_pushnil(state);
            int cachesets = API.lua_gettop(state);
            into(state, this);
            int thismeta = API.lua_gettop(state);
            if (HasMethod())
            {
                API.lua_pushvalue(state, thismeta);
                API.lua_pushcclosure(state, __method, 1);
            }
            else
            {
                API.lua_pushnil(state);
            }
            int cmethod = API.lua_gettop(state);
            if (HasGet())
            {
                API.lua_pushvalue(state, thismeta);
                API.lua_pushcclosure(state, __get, 1);
            }
            else
            {
                API.lua_pushnil(state);
            }
            int cget = API.lua_gettop(state);
            if (HasSet())
            {
                API.lua_pushvalue(state, thismeta);
                API.lua_pushcclosure(state, __set, 1);
            }
            else
            {
                API.lua_pushnil(state);
            }
            int cset = API.lua_gettop(state);
            if (HasGetStatic())
            {
                API.lua_pushvalue(state, thismeta);
                API.lua_pushcclosure(state, __gets, 1);
            }
            else
            {
                API.lua_pushnil(state);
            }
            int cgets = API.lua_gettop(state);
            if (HasSetStatic())
            {
                API.lua_pushvalue(state, thismeta);
                API.lua_pushcclosure(state, __sets, 1);
            }
            else
            {
                API.lua_pushnil(state);
            }
            int csets = API.lua_gettop(state);
            if (HasGetIndex())
            {
                API.lua_pushvalue(state, thismeta);
                API.lua_pushcclosure(state, __index, 1);
            }
            else
            {
                API.lua_pushnil(state);
            }
            int cindex = API.lua_gettop(state);
            if (HasSetIndex())
            {
                API.lua_pushvalue(state, thismeta);
                API.lua_pushcclosure(state, __newindex, 1);
            }
            else
            {
                API.lua_pushnil(state);
            }
            int cnewindex = API.lua_gettop(state);
            firstInit(state, name);

            genindexs.Push(state);
            API.lua_pushvalue(state, typetable);
            API.lua_pushvalue(state, cachegets);
            API.lua_pushvalue(state, cmethod);
            API.lua_pushvalue(state, cgets);
            API.lua_pcall(state, 4, 1, 0);
            int findexs = API.lua_gettop(state);

            gennewindexs.Push(state);
            API.lua_pushvalue(state, typetable);
            API.lua_pushvalue(state, cachesets);
            API.lua_pushvalue(state, csets);
            API.lua_pcall(state, 3, 1, 0);
            int fnewindexs = API.lua_gettop(state);

            genindex.Push(state);
            API.lua_pushvalue(state, typetable);
            API.lua_pushvalue(state, cacheget);
            API.lua_pushvalue(state, cmethod);
            API.lua_pushvalue(state, cget);
            API.lua_pushvalue(state, cindex);
            API.lua_pushvalue(state, findexs);
            API.lua_pcall(state, 6, 1, 0);
            int findex = API.lua_gettop(state);

            gennewindex.Push(state);
            API.lua_pushvalue(state, typetable);
            API.lua_pushvalue(state, cacheset);
            API.lua_pushvalue(state, cset);
            API.lua_pushvalue(state, cnewindex);
            API.lua_pushvalue(state, fnewindexs);
            API.lua_pcall(state, 5, 1, 0);
            int fnewindex = API.lua_gettop(state);

            API.lua_pushliteral(state, "__index");
            if (HasMethod() || HasGet() || HasGetStatic() || HasGetIndex())
                API.lua_pushvalue(state, findex);
            else
                API.lua_pushnil(state);
            API.lua_settable(state, metatable);
            API.lua_pushliteral(state, "__newindex");
            if (HasMethod() || HasSet() || HasSetStatic() || HasSetIndex())
                API.lua_pushvalue(state, fnewindex);
            else
                API.lua_pushnil(state);
            API.lua_settable(state, metatable);
            if (HasNew() || HasMethod() || HasGetStatic() || HasSetStatic())
            {
                API.lua_newtable(state);
                API.lua_pushvalue(state, -1);
                API.lua_setmetatable(state, typetable);
                if (HasMethod() || HasGetStatic())
                {
                    API.lua_pushliteral(state, "__index");
                    API.lua_pushvalue(state, findexs);
                    API.lua_settable(state, -3);
                }
                if (HasSetStatic())
                {
                    API.lua_pushliteral(state, "__newindex");
                    API.lua_pushvalue(state, fnewindexs);
                    API.lua_settable(state, -3);
                }
                if (HasNew())
                {
                    int top = API.lua_gettop(state);
                    API.lua_pushliteral(state, "__call");
                    if (GetNewFunction(state))
                    {
                        API.lua_settop(state, top + 2);
                        API.lua_settable(state, top);
                    }
                    else
                    {
                        API.lua_settop(state, top);
                    }
                }
            }
            else
            {
                API.lua_pushnil(state);
                API.lua_setmetatable(state, typetable);
            }
            API.lua_settop(state, typetable);
            InitConst(state, typetable);
            if (GetAddFunction(state))
            {
                API.lua_pushliteral(state, "__add");
                API.lua_insert(state, -2);
                API.lua_settable(state, metatable);
            }
            if (GetSubFunction(state))
            {
                API.lua_pushliteral(state, "__sub");
                API.lua_insert(state, -2);
                API.lua_settable(state, metatable);
            }
            if (GetMulFunction(state))
            {
                API.lua_pushliteral(state, "__mul");
                API.lua_insert(state, -2);
                API.lua_settable(state, metatable);
            }
            if (GetDivFunction(state))
            {
                API.lua_pushliteral(state, "__div");
                API.lua_insert(state, -2);
                API.lua_settable(state, metatable);
            }
            if (GetModFunction(state))
            {
                API.lua_pushliteral(state, "__mod");
                API.lua_insert(state, -2);
                API.lua_settable(state, metatable);
            }
            if (GetUnmFunction(state))
            {
                API.lua_pushliteral(state, "__unm");
                API.lua_insert(state, -2);
                API.lua_settable(state, metatable);
            }
            if (GetEqualFunction(state))
            {
                API.lua_pushliteral(state, "__eq");
                API.lua_insert(state, -2);
                API.lua_settable(state, metatable);
            }
            if (GetLessThanFunction(state))
            {
                API.lua_pushliteral(state, "__lt");
                API.lua_insert(state, -2);
                API.lua_settable(state, metatable);
            }
            if (GetLessEqualFunction(state))
            {
                API.lua_pushliteral(state, "__le");
                API.lua_insert(state, -2);
                API.lua_settable(state, metatable);
            }
            if (GetCallFunction(state))
            {
                API.lua_pushliteral(state, "__call");
                API.lua_insert(state, -2);
                API.lua_settable(state, metatable);
            }
        }
    }

    public sealed class State2
    {
        public class ReflectType : MetaType
        {
            protected Type type;
            protected bool hascost;
            protected bool hasnew;
            protected bool hasmethod;
            protected bool hasget;
            protected bool hasset;
            protected bool hasgets;
            protected bool hassets;
            protected bool hasindex;
            protected bool hasnewindex;

            public ReflectType(Type t)
            {
                type = t;
                foreach (ConstructorInfo constructor in type.GetConstructors())
                {
                    if (constructor.IsGenericMethod)
                        continue;
                    hasnew = true;
                }
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (field.IsStatic && (field.IsLiteral || field.IsInitOnly))
                    {
                        hascost = true;
                    }
                    else
                    {
                        if (field.IsStatic)
                        {
                            hasgets = true;
                            hassets = true;
                        }
                        else
                        {
                            hasget = true;
                            hasset = true;
                        }
                    }
                }
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                {
                    ParameterInfo[] indexparams = property.GetIndexParameters();
                    if (indexparams != null && indexparams.Length > 0)
                    {
                        if (property.CanRead)
                        {
                            MethodInfo method = property.GetGetMethod();
                            if (method != null)
                            {
                                hasindex = true;
                            }
                        }
                        if (property.CanWrite)
                        {
                            MethodInfo method = property.GetSetMethod();
                            if (method != null)
                            {
                                hasnewindex = true;
                            }
                        }
                    }
                    else
                    {
                        if (property.CanRead)
                        {
                            MethodInfo method = property.GetGetMethod();
                            if (method != null)
                            {
                                if (method.IsStatic)
                                {
                                    hasgets = true;
                                }
                                else
                                {
                                    hasget = true;
                                }
                            }
                        }
                        if (property.CanWrite)
                        {
                            MethodInfo method = property.GetSetMethod();
                            if (method != null)
                            {
                                if (method.IsStatic)
                                {
                                    hassets = true;
                                }
                                else
                                {
                                    hasset = true;
                                }
                            }
                        }
                    }
                }
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (method.IsGenericMethod)
                        continue;
                    if (!method.IsStatic || !method.IsSpecialName || method.Name.Substring(0, "op_".Length) != "op_")
                    {
                        hasmethod = true;
                    }
                }
            }

            protected override void InitConst(State2 state, int typetable)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (field.IsStatic && (field.IsLiteral || field.IsInitOnly))
                    {
                        API.lua_pushliteral(state, field.Name);
                        state.PushType(field.GetValue(type), field.FieldType);
                        API.lua_settable(state, typetable);
                    }
                }
            }
            protected override bool HasNew() { return hasnew; }
            protected override bool HasMethod() { return hasmethod; }
            protected override bool HasGet() { return hasget; }
            protected override bool HasSet() { return hasset; }
            protected override bool HasGetStatic() { return hasgets; }
            protected override bool HasSetStatic() { return hassets; }
            protected override bool HasGetIndex() { return hasindex; }
            protected override bool HasSetIndex() { return hasnewindex; }

            [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
            private static int __new(IntPtr L)
            {
                return 0;
            }

            protected override bool GetNewFunction(State2 state) { return false; }
            protected override bool GetMethodFunction(State2 state, string name) { return false; }
            protected override bool GetGetFunction(State2 state, string name) { return false; }
            protected override bool GetSetFunction(State2 state, string name) { return false; }
            protected override bool GetGetStaticFunction(State2 state, string name) { return false; }
            protected override bool GetSetStaticFunction(State2 state, string name) { return false; }
            protected override bool GetIndex(State2 state) { return false; }
            protected override bool SetIndex(State2 state) { return false; }
            protected override bool GetAddFunction(State2 state) { return false; }
            protected override bool GetSubFunction(State2 state) { return false; }
            protected override bool GetMulFunction(State2 state) { return false; }
            protected override bool GetDivFunction(State2 state) { return false; }
            protected override bool GetModFunction(State2 state) { return false; }
            protected override bool GetUnmFunction(State2 state) { return false; }
            protected override bool GetEqualFunction(State2 state) { return false; }
            protected override bool GetLessThanFunction(State2 state) { return false; }
            protected override bool GetLessEqualFunction(State2 state) { return false; }
        }

        private class ReflectValueType : ReflectType
        {
            public ReflectValueType(Type type) : base(type) { }
        }

        private class ReflectDelegateType : MetaType
        {
            public ReflectDelegateType(Type type) { }
        }
        private class FunctionImpl : Function
        {
            private int refidx;
            private State2 refstate;
            public FunctionImpl(IntPtr L, int idx)
            {
                refstate = L;
                API.lua_pushvalue(L, idx);
                refidx = API.lua_ref(L);
            }
            ~FunctionImpl()
            {
				Finalizes.Instance.Add(delegate()
				{
					API.lua_unref(refstate, refidx);
				});
            }
            public override bool Push(IntPtr L)
            {
                if (refstate.L != API.lua_host(L))
                    return false;
                API.lua_getref(L, refidx);
                return true;
            }
            public override object[] Call(int nresults, params object[] args)
            {
                int top = API.lua_gettop(refstate);
                API.lua_checkstack(refstate, args.Length + top + 1);
                API.lua_getref(refstate, refidx);
                foreach (object arg in args)
                {
                    refstate.PushType(arg);
                }
                if (API.lua_pcall(refstate, args.Length, nresults, 0) != 0)
                    throw new Error(API.lua_tostring(refstate, -1));
                object[] returns = new object[API.lua_gettop(refstate) - top];
                for (int i = 0; i < returns.Length; ++i)
                {
                    refstate.ToType(top + i + 1, ref returns[i]);
                }
                API.lua_settop(refstate, top);
                return returns;
            }
        }
        private class TableImpl : Table
        {
            private int refidx;
            private State2 refstate;
            public TableImpl(IntPtr L, int idx)
            {
                refstate = L;
                API.lua_pushvalue(L, idx);
                refidx = API.lua_ref(L);
            }
            ~TableImpl()
            {
				Finalizes.Instance.Add(delegate()
				{
					API.lua_unref(refstate, refidx);
				});
            }
            public override bool Push(IntPtr L)
            {
                if (refstate.L != API.lua_host(L))
                    return false;
                API.lua_getref(L, refidx);
                return true;
            }
        }
        private class ThreadImpl : Thread
        {
            private int refidx;
            private State2 refstate;
            public ThreadImpl(IntPtr L, int idx)
            {
                refstate = L;
                API.lua_pushvalue(L, idx);
                refidx = API.lua_ref(L);
            }
            ~ThreadImpl()
            {
				Finalizes.Instance.Add(delegate()
				{
					API.lua_unref(refstate, refidx);
				});
            }
            public override bool Push(IntPtr L)
            {
                if (refstate.L != API.lua_host(L))
                    return false;
                API.lua_getref(L, refidx);
                return true;
            }
            public override object[] Call(int nresults, params object[] args)
            {
                int top = API.lua_gettop(refstate);
                IntPtr L = API.lua_tothread(refstate, -1);
                API.lua_pop(refstate, 1);
                API.lua_checkstack(refstate, args.Length + top);
                foreach (object arg in args)
                {
                    refstate.PushType(arg);
                }
                API.lua_checkstack(L, args.Length + API.lua_gettop(L));
                API.lua_xmove(refstate, L, args.Length);
                int result = API.lua_resume(L, args.Length);
                if (result != 0 && result != Consts.LUA_YIELD)
                    throw new Error(API.lua_tostring(L, -1));
                if (nresults == Consts.LUA_MULTRET)
                    nresults = API.lua_gettop(L);
                API.lua_settop(L, nresults);
                object[] returns = new object[nresults];
                top = API.lua_gettop(refstate);
                API.lua_checkstack(refstate, returns.Length + top);
                API.lua_xmove(L, refstate, returns.Length);
                for (int i = 0; i < returns.Length; ++i)
                {
                    refstate.ToType(top + i + 1, ref returns[i]);
                }
                API.lua_settop(refstate, top);
                return returns;
            }
        }
        private class TypeInfo
        {
            public string name;
            public Type type;
            public MetaType metatype;
            public GCHandle gch;
        }
        private IntPtr L;
        private luaL_Key weaktablekey;
        private Dictionary<Type, TypeInfo> typenames;
        private static Dictionary<object, GCHandle> refptrs;

        public State2()
        {
            L = API.lua_open();
            GCHandle gchthis = GCHandle.Alloc(this, GCHandleType.Weak);
            API.lua_setrefers(L, (IntPtr)gchthis);
            API.luaL_initlibs(L);
            API.luaL_openlibs(L);
            typenames = new Dictionary<Type, TypeInfo>();
            refptrs = new Dictionary<object, GCHandle>();
        }
        ~State2()
        {
			Finalizes.Instance.Add(delegate()
			{
				GCHandle gchthis = (GCHandle)API.lua_getrefers(L);
				gchthis.Free();
				foreach (KeyValuePair<Type, TypeInfo> pair in typenames)
				{
					pair.Value.gch.Free();
				}
				typenames.Clear();
				foreach (KeyValuePair<object, GCHandle> pair in refptrs)
				{
					pair.Value.Free();
				}
				refptrs.Clear();
				API.lua_close(L);
			});
        }
        public static implicit operator IntPtr(State2 state)
        {
            return state.L;
        }
        public static implicit operator State2(IntPtr L)
        {
            GCHandle gchthis = (GCHandle)API.lua_getrefers(L);
            return (State2)gchthis.Target;
        }

        private void getweaktable()
        {
            if (weaktablekey == null)
            {
                API.lua_newtable(L);
                weaktablekey = new luaL_Key();
                API.lua_pushlightuserdata(L, weaktablekey);
                API.lua_pushvalue(L, -2);
                API.lua_settable(L, Consts.LUA_REGISTRYINDEX);
                API.lua_createtable(L, 1, 0);
                API.lua_pushliteral(L, "__mode");
                API.lua_pushliteral(L, "v");
                API.lua_settable(L, -3);
                API.lua_setmetatable(L, -2);
            }
            else
            {
                API.lua_pushlightuserdata(L, weaktablekey);
                API.lua_gettable(L, Consts.LUA_REGISTRYINDEX);
            }
        }

        private void findtable(string name)
        {
            API.lua_pushvalue(L, Consts.LUA_GLOBALSINDEX);
            string[] names = name.Split('.');
            foreach (string s in names)
            {
                API.lua_pushliteral(L, s);
                API.lua_pushvalue(L, -1);
                API.lua_gettable(L, -3);
                if (!API.lua_istable(L, -1))
                {
                    API.lua_pop(L, 1);
                    API.lua_createtable(L, 0, 1);
                    API.lua_pushvalue(L, -2);
                    API.lua_pushvalue(L, -2);
                    API.lua_settable(L, -5);
                }
                API.lua_replace(L, -3);
                API.lua_pop(L, 1);
            }
        }

        private TypeInfo newtype(Type type)
        {
            TypeInfo typeinfo = new TypeInfo();
            typeinfo.name = null;
            typeinfo.type = type;
            typeinfo.metatype = null;
            typeinfo.gch = GCHandle.Alloc(type);
            return typeinfo;
        }

        private TypeInfo gettypeof(Type type)
        {
            TypeInfo typeinfo;
            if (!typenames.TryGetValue(type, out typeinfo))
            {
                typeinfo = newtype(type);
                API.lua_pushlightuserdata(L, (IntPtr)(typeinfo.gch));
                API.lua_newtable(L);
                API.lua_settable(L, Consts.LUA_REGISTRYINDEX);
                typenames.Add(type, typeinfo);
            }
            return typeinfo;
        }

        public MetaType MetaTypeOf(Type type)
        {
            TypeInfo typeinfo;
            if (!typenames.TryGetValue(type, out typeinfo))
                return null;
            return typeinfo.metatype;
        }

        public Function Compile(byte[] bytes, string name)
        {
            if (API.luaL_loadbuffer(L, bytes, name) != 0)
            {
                API.lua_pop(L, 1);
                return null;
            }
            Function f = null;
            ToType(-1, ref f);
            API.lua_pop(L, 1);
            return f;
        }

        public Function Compile(string str, string name)
        {
            byte[] bytes = System.Text.Encoding.Default.GetBytes(str);
            if (System.Text.Encoding.Default != System.Text.Encoding.UTF8)
            {
                bytes = System.Text.Encoding.Convert(System.Text.Encoding.Default, System.Text.Encoding.UTF8, bytes);
            }
            return Compile(bytes, name);
        }

        public Function Compile(string str)
        {
            if (API.luaL_loadstring(L, str) != 0)
            {
                API.lua_pop(L, 1);
                return null;
            }
            Function f = null;
            ToType(-1, ref f);
            API.lua_pop(L, 1);
            return f;
        }

        public Thread CreateThread(Function f)
        {
            IntPtr nL = API.lua_newthread(L);
            if (!f.Push(nL))
            {
                API.lua_pop(L, 1);
                return null;
            }
            Thread t = new ThreadImpl(L, -1);
            API.lua_pop(L, 1);
            return t;
        }

        public bool Import(Type type, string name, MetaType metatype)
        {
            TypeInfo typeinfo = gettypeof(type);
            if (typeinfo.name != null && typeinfo.name != "")
            {
                API.lua_pushvalue(L, Consts.LUA_GLOBALSINDEX);
                string[] names = typeinfo.name.Split('.');
                for (int i = 0; i < names.Length - 1; ++i)
                {
                    API.lua_pushliteral(L, names[i]);
                    API.lua_gettable(L, -2);
                    API.lua_replace(L, -2);
                    if (!API.lua_istable(L, -1))
                    {
                        break;
                    }
                }
                if (API.lua_istable(L, -1))
                {
                    API.lua_pushliteral(L, names[names.Length - 1]);
                    API.lua_pushnil(L);
                    API.lua_settable(L, -3);
                }
                API.lua_pop(L, 1);
            }
            typeinfo.name = name;
            typeinfo.metatype = metatype;
            if (metatype != null)
            {
                int top = API.lua_gettop(L);
                API.lua_pushlightuserdata(L, (IntPtr)(typeinfo.gch));
                API.lua_gettable(L, Consts.LUA_REGISTRYINDEX);
                findtable(name);
                metatype.Init(this, type, name);
                API.lua_settop(L, top);
            }

            findtable("Types");
            API.lua_pushliteral(L, name);
            PushType(type);
            API.lua_settable(L, -3);
            API.lua_pop(L, 1);
            return true;
        }

        public bool Import(Type type, MetaType metatype)
        {
            return Import(type, Utils.NameOf(type), metatype);
        }

        public bool Import(string name, MetaType metatype)
        {
            Type type = Utils.FindType(name);
            if (type == null)
                return false;
            return Import(type, name, metatype);
        }

        public bool Import(Type type, string name)
        {
            TypeInfo typeinfo = gettypeof(type);
            if (typeinfo.name != null && typeinfo.name != "")
            {
                return true;
            }
            if (type.IsEnum)
            {
                typeinfo.name = name;
                Type utype = Enum.GetUnderlyingType(type);
                string[] names = Enum.GetNames(type);
                if (utype == typeof(ulong) || utype == typeof(long))
                {
                    foreach (string item in names)
                    {
                        PushType(item);
                        PushType(Convert.ToUInt64(Enum.Parse(utype, item)));
                    }
                }
                else
                {
                    foreach (string item in names)
                    {
                        PushType(item);
                        PushType(Convert.ToDouble(Enum.Parse(utype, item)));
                    }
                }
                return true;
            }
            MetaType metatype;
            if (type.IsValueType)
            {
                metatype = new ReflectValueType(type);
            }
            else if (type.IsSubclassOf(typeof(Delegate)))
            {
                metatype = new ReflectDelegateType(type);
            }
            else
            {
                metatype = new ReflectType(type);
            }
            return Import(type, name, metatype);
        }

        public bool Import(Type type)
        {
            return Import(type, Utils.NameOf(type));
        }

        public bool Import(string name)
        {
            Type type = Utils.FindType(name);
            if (type == null)
                return false;
            return Import(type, name);
        }

        

        public void FreeRef(object o)
        {
            refptrs.Remove(o);
        }
    }

    public static class helper
    {
        private class TypeInfo
        {
            public string name;
            public Type type;
            public GCHandle gch;
            public Dictionary<Type, bool> parents;
        }
        private static Dictionary<Type, TypeInfo> typenames = new Dictionary<Type, TypeInfo>();
        private static bool typeinit = false;

        private static Dictionary<object, GCHandle> refptrs = new Dictionary<object, GCHandle>();
        private static IntPtr weaktablekey = IntPtr.Zero;
        private static void getweaktable(IntPtr L)
        {
            if (weaktablekey == IntPtr.Zero)
            {
                API.lua_newtable(L);
                weaktablekey = Marshal.AllocHGlobal(1);
                API.lua_pushlightuserdata(L, weaktablekey);
                API.lua_pushvalue(L, -2);
                API.lua_settable(L, Consts.LUA_REGISTRYINDEX);
                API.lua_createtable(L, 1, 0);
                API.lua_pushliteral(L, "__mode");
                API.lua_pushliteral(L, "v");
                API.lua_settable(L, -3);
                API.lua_setmetatable(L, -2);
            }
            else
            {
                API.lua_pushlightuserdata(L, weaktablekey);
                API.lua_gettable(L, Consts.LUA_REGISTRYINDEX);
            }
        }

        private static void findtypetable(IntPtr L, string name)
        {
            API.lua_pushvalue(L, Consts.LUA_GLOBALSINDEX);
            string[] names = name.Split('.');
            foreach (string s in names)
            {
                API.lua_pushliteral(L, s);
                API.lua_pushvalue(L, -1);
                API.lua_gettable(L, -3);
                if (!API.lua_istable(L, -1))
                {
                    API.lua_pop(L, 1);
                    API.lua_createtable(L, 0, 1);
                    API.lua_pushvalue(L, -2);
                    API.lua_pushvalue(L, -2);
                    API.lua_settable(L, -5);
                }
                API.lua_replace(L, -3);
                API.lua_pop(L, 1);
            }
        }

        private enum tables : int
        {
            meta = 1,
            list,
            index,
            newindex,
            value,
            newvalue,

            type,
        }

        public struct luaclass : IDisposable
        {
            private IntPtr L;
            private int top;
            private TypeInfo typeinfo;

            private void init(IntPtr L, string name, Type type, Type[] parents)
            {
                if (!typeinit)
                {
                    typeinit = true;

                    using (luaclass tolua = new luaclass(L, "System.Type", typeof(System.Type)))
                    {
                    }
                }
                if (typenames.TryGetValue(type, out typeinfo))
                {
                    API.lua_pushlightuserdata(L, (IntPtr)typeinfo.gch);
                    API.lua_gettable(L, Consts.LUA_REGISTRYINDEX);
                }
                else
                {
                    typeinfo = new TypeInfo();
                    typeinfo.name = name;
                    typeinfo.type = type;
                    typeinfo.gch = GCHandle.Alloc(type);
                    typeinfo.parents = new Dictionary<Type, bool>();
                    typenames[type] = typeinfo;
                    API.lua_createtable(L, 0, 1);
                    int metatable = API.lua_gettop(L);
                    API.lua_pushlightuserdata(L, (IntPtr)typeinfo.gch);
                    API.lua_pushvalue(L, metatable);
                    API.lua_settable(L, Consts.LUA_REGISTRYINDEX);
                    API.lua_pushvalue(L, metatable);
                    API.lua_rawseti(L, metatable, (int)tables.meta);
                    findtypetable(L, name);
                    int listtable = API.lua_gettop(L);
                    API.lua_pushvalue(L, listtable);
                    API.lua_rawseti(L, metatable, (int)tables.list);
                    API.lua_newtable(L);
                    int metalisttable = API.lua_gettop(L);
                    API.lua_pushvalue(L, metalisttable);
                    API.lua_setmetatable(L, listtable);
                    API.lua_newtable(L);
                    int indextable = API.lua_gettop(L);
                    API.lua_pushvalue(L, indextable);
                    API.lua_rawseti(L, metatable, (int)tables.index);
                    API.lua_newtable(L);
                    int newindextable = API.lua_gettop(L);
                    API.lua_pushvalue(L, newindextable);
                    API.lua_rawseti(L, metatable, (int)tables.newindex);
                    API.lua_newtable(L);
                    int valuetable = API.lua_gettop(L);
                    API.lua_pushvalue(L, valuetable);
                    API.lua_rawseti(L, metatable, (int)tables.value);
                    API.lua_newtable(L);
                    int newvaluetable = API.lua_gettop(L);
                    API.lua_pushvalue(L, newvaluetable);
                    API.lua_rawseti(L, metatable, (int)tables.newvalue);

                    API.lua_pushliteral(L, "__index");
                    API.lua_pushvalue(L, valuetable);
                    API.lua_pushvalue(L, listtable);
                    API.lua_pushcclosure(L, __value, 2);
                    API.lua_settable(L, metalisttable);
                    API.lua_pushliteral(L, "__newindex");
                    API.lua_pushvalue(L, newvaluetable);
                    API.lua_pushvalue(L, listtable);
                    API.lua_pushcclosure(L, __newvalue, 2);
                    API.lua_settable(L, metalisttable);

                    API.lua_pushlightuserdata(L, (IntPtr)typeinfo.gch);
                    API.lua_rawseti(L, metatable, (int)tables.type);

                    TypeInfo parenttypeinfo;
                    foreach (Type parent in parents)
                    {
                        if (typenames.TryGetValue(parent, out parenttypeinfo))
                        {
                            typeinfo.parents[parent] = true;
                            foreach (KeyValuePair<System.Type, bool> pair in parenttypeinfo.parents)
                            {
                                typeinfo.parents[pair.Key] = true;
                            }
                            API.lua_pushlightuserdata(L, (IntPtr)parenttypeinfo.gch);
                            API.lua_gettable(L, Consts.LUA_REGISTRYINDEX);
                            int parentmetatable = API.lua_gettop(L);
                            API.lua_pushnil(L);
                            while (API.lua_next(L, parentmetatable))
                            {
                                if (API.lua_type(L, -2) == Consts.LUA_TSTRING)
                                {
                                    API.lua_pushvalue(L, -2);
                                    API.lua_insert(L, -2);
                                    API.lua_rawset(L, metatable);
                                }
                                else
                                {
                                    API.lua_pop(L, 1);
                                }
                            }
                            for (int i = (int)tables.list; i < (int)tables.type; ++i)
                            {
                                API.lua_rawgeti(L, metatable, i);
                                API.lua_rawgeti(L, parentmetatable, i);
                                API.lua_pushnil(L);
                                while (API.lua_next(L, -2))
                                {
                                    API.lua_pushvalue(L, -2);
                                    API.lua_insert(L, -2);
                                    API.lua_rawset(L, -5);
                                }
                                API.lua_pop(L, 2);
                            }
                        }
                    }

                    API.lua_pushliteral(L, "__type");
                    API.lua_pushliteral(L, name);
                    API.lua_pushcclosure(L, __type, 1);
                    API.lua_settable(L, metatable);
                    API.lua_pushliteral(L, "__gc");
                    API.lua_pushcfunction(L, __gc);
                    API.lua_settable(L, metatable);
                    API.lua_pushliteral(L, "__index");
                    API.lua_pushvalue(L, indextable);
                    API.lua_pushvalue(L, listtable);
                    API.lua_pushcclosure(L, __index, 2);
                    API.lua_settable(L, metatable);
                    API.lua_pushliteral(L, "__newindex");
                    API.lua_pushvalue(L, newindextable);
                    API.lua_pushvalue(L, listtable);
                    API.lua_pushcclosure(L, __newindex, 2);
                    API.lua_settable(L, metatable);
                    API.lua_pushliteral(L, "type");
                    pushtype(L, type, typeof(System.Type));
                    API.lua_rawset(L, listtable);
                    
                    API.lua_settop(L, metatable);
                }
                for (int i = (int)tables.list; i < (int)tables.type; ++i)
                {
                    API.lua_rawgeti(L, top + (int)tables.meta, i);
                }
            }

            public luaclass(IntPtr L, string name, Type type, params Type[] parents)
            {
                this.top = API.lua_gettop(L);
                this.L = L;
                this.typeinfo = null;
                init(L, name, type, parents);
            }

            public luaclass(IntPtr L, string name, lua_CFunction creater, Type type, params Type[] parents)
            {
                this.top = API.lua_gettop(L);
                this.L = L;
                this.typeinfo = null;
                init(L, name, type, parents);
                API.lua_getmetatable(L, top + (int)tables.list);
                API.lua_pushliteral(L, "__call");
                API.lua_pushcfunction(L, creater);
                API.lua_settable(L, -3);
                API.lua_pop(L, 1);
            }

            public void Dispose()
            {
                API.lua_settop(L, top);
            }

            public void setfields(string field, params lua_CFunction[] funcs)
            {
                string[] fields = field.Split();
                for (int i = 0, j = 0; i < fields.Length && j < funcs.Length; ++i, ++j)
                {
                    while (fields[i] == "")
                    {
                        ++i;
                        break;
                    }
                    API.lua_pushliteral(L, fields[i]);
                    API.lua_pushcfunction(L, funcs[j]);
                    API.lua_pushliteral(L, "set" + fields[i]);
                    API.lua_pushvalue(L, -2);
                    API.lua_rawset(L, top + (int)tables.list);
                    API.lua_rawset(L, top + (int)tables.newindex);
                }
            }

            public void getfields(string field, params lua_CFunction[] funcs)
            {
                string[] fields = field.Split();
                for (int i = 0, j = 0; i < fields.Length && j < funcs.Length; ++i, ++j)
                {
                    while (fields[i] == "")
                    {
                        ++i;
                        break;
                    }
                    API.lua_pushliteral(L, fields[i]);
                    API.lua_pushcfunction(L, funcs[j]);
                    API.lua_pushliteral(L, "get" + fields[i]);
                    API.lua_pushvalue(L, -2);
                    API.lua_rawset(L, top + (int)tables.list);
                    API.lua_rawset(L, top + (int)tables.index);
                }
            }

            public void setvalues(string field, params lua_CFunction[] funcs)
            {
                string[] fields = field.Split();
                for (int i = 0, j = 0; i < fields.Length && j < funcs.Length; ++i, ++j)
                {
                    while (fields[i] == "")
                    {
                        ++i;
                        break;
                    }
                    API.lua_pushliteral(L, fields[i]);
                    API.lua_pushcfunction(L, funcs[j]);
                    API.lua_pushliteral(L, "set" + fields[i]);
                    API.lua_pushvalue(L, -2);
                    API.lua_rawset(L, top + (int)tables.list);
                    API.lua_rawset(L, top + (int)tables.newvalue);
                }
            }

            public void getvalues(string field, params lua_CFunction[] funcs)
            {
                string[] fields = field.Split();
                for (int i = 0, j = 0; i < fields.Length && j < funcs.Length; ++i, ++j)
                {
                    while (fields[i] == "")
                    {
                        ++i;
                        break;
                    }
                    API.lua_pushliteral(L, fields[i]);
                    API.lua_pushcfunction(L, funcs[j]);
                    API.lua_pushliteral(L, "get" + fields[i]);
                    API.lua_pushvalue(L, -2);
                    API.lua_rawset(L, top + (int)tables.list);
                    API.lua_rawset(L, top + (int)tables.value);
                }
            }

            public void methods(string field, params lua_CFunction[] funcs)
            {
                string[] fields = field.Split();
                for (int i = 0, j = 0; i < fields.Length && j < funcs.Length; ++i, ++j)
                {
                    while (fields[i] == "")
                    {
                        ++i;
                        break;
                    }
                    API.lua_pushliteral(L, fields[i]);
                    API.lua_pushcfunction(L, funcs[j]);
                    API.lua_rawset(L, top + (int)tables.list);
                }
            }

            public void opts(string field, params lua_CFunction[] funcs)
            {
                string[] fields = field.Split();
                for (int i = 0, j = 0; i < fields.Length && j < funcs.Length; ++i, ++j)
                {
                    while (fields[i] == "")
                    {
                        ++i;
                        break;
                    }
                    API.lua_pushliteral(L, fields[i]);
                    API.lua_pushcfunction(L, funcs[j]);
                    API.lua_rawset(L, top + (int)tables.meta);
                }
            }

            public void consts(string field, params object[] objs)
            {
                string[] fields = field.Split();
                for (int i = 0, j = 0; i < fields.Length && j < objs.Length; ++i, ++j)
                {
                    while (fields[i] == "")
                    {
                        ++i;
                        break;
                    }
                    API.lua_pushliteral(L, fields[i]);
                    pushtype(L, objs[i]);
                    API.lua_rawset(L, top + (int)tables.list);
                }
            }
        }

        public static void methods(IntPtr L, string name, string field, params lua_CFunction[] funcs)
        {
            string[] fields = field.Split();
            findtypetable(L, name);
            for (int i = 0, j = 0; i < fields.Length && j < funcs.Length; ++i, ++j)
            {
                while (fields[i] == "")
                {
                    ++i;
                    break;
                }
                API.lua_pushliteral(L, fields[i]);
                API.lua_pushcfunction(L, funcs[j]);
                API.lua_rawset(L, -3);
            }
            API.lua_pop(L, 1);
        }

        public static void consts(IntPtr L, string name, string field, params object[] objs)
        {
            string[] fields = field.Split();
            findtypetable(L, name);
            for (int i = 0, j = 0; i < fields.Length && j < objs.Length; ++i, ++j)
            {
                while (fields[i] == "")
                {
                    ++i;
                    break;
                }
                API.lua_pushliteral(L, fields[i]);
                pushtype(L, objs[i]);
                API.lua_rawset(L, -3);
            }
            API.lua_pop(L, 1);
        }

        public static bool totype(IntPtr L, int idx, ref int n)
        {
            if (!API.lua_isnumber(L, idx))
                return false;
            n = API.lua_tointeger(L, idx);
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref uint n)
        {
            if (!API.lua_isnumber(L, idx))
                return false;
            n = (uint)(API.lua_tonumber(L, idx));
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref short n)
        {
            if (!API.lua_isnumber(L, idx))
                return false;
            n = (short)API.lua_tointeger(L, idx);
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref ushort n)
        {
            if (!API.lua_isnumber(L, idx))
                return false;
            n = (ushort)API.lua_tointeger(L, idx);
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref byte n)
        {
            if (!API.lua_isnumber(L, idx))
                return false;
            n = (byte)API.lua_tointeger(L, idx);
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref sbyte n)
        {
            if (!API.lua_isnumber(L, idx))
                return false;
            n = (sbyte)API.lua_tointeger(L, idx);
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref char n)
        {
            if (!API.lua_isnumber(L, idx))
                return false;
            n = (char)API.lua_tointeger(L, idx);
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref double d)
        {
            if (!API.lua_isnumber(L, idx))
                return false;
            d = API.lua_tonumber(L, idx);
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref float f)
        {
            if (!API.lua_isnumber(L, idx))
                return false;
            f = (float)API.lua_tonumber(L, idx);
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref bool b)
        {
			b = API.lua_toboolean(L, idx);
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref string s)
        {
            if (!API.lua_isstring(L, idx))
                return false;
            s = API.lua_tostring(L, idx);
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref ulong l)
        {
            if (!API.lua_isstring(L, idx))
                return false;
            int len;
            IntPtr ptr = API.lua_tolstring(L, idx, out len);
            if (ptr == IntPtr.Zero || len != 9)
                return false;
            byte[] bytes = new byte[9];
            Marshal.Copy(ptr, bytes, 0, 9);
            if (bytes[0] != 76)
                return false;
            l = 0;
            for (int i = 1; i < 9; ++i)
            {
                l = (l << 8) + bytes[i];
            }
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref long l)
        {
            ulong u = 0;
            if (!totype(L, idx, ref u))
                return false;
            l = (long)u;
            return true;
        }

        public static bool totype<T>(IntPtr L, int idx, ref T t)
        {
            object o = null;
            if (!totype(L, idx, ref o, typeof(T)))
                return false;

            try
            {
                t = (T)o;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool totype(IntPtr L, int idx, ref object o, Type type)
        {
            if (type.IsArray)
            {
                if (!API.lua_istable(L, idx))
                    return false;

                type = type.GetElementType();
                int len = API.lua_objlen(L, idx).ToInt32();
                Array array = Array.CreateInstance(type, len);
                object item = null;
                for (int i = 0; i < len; ++i)
                {
                    API.lua_rawgeti(L, idx, i + 1);
                    if (!totype(L, -1, ref item, type))
                    {
                        API.lua_pop(L, 1);
                        return false;
                    }
                    try
                    {
                        array.SetValue(item, i);
                    }
                    catch
                    {
                        API.lua_pop(L, 1);
                        return false;
                    }
                    API.lua_pop(L, 1);
                }
                o = array;
                return true;
            }
            else if (type.IsPrimitive)
            {
                if (type == typeof(bool))
                {
                    bool b = default(bool);
                    if (!totype(L, idx, ref b))
                        return false;
                    o = b;
                    return true;
                }
                else if (type == typeof(int))
                {
                    int d = default(int);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = d;
                    return true;
                }
                else if (type == typeof(uint))
                {
                    uint d = default(uint);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = d;
                    return true;
                }
                else if (type == typeof(float))
                {
                    float d = default(float);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = d;
                    return true;
                }
                else if (type == typeof(double))
                {
                    double d = default(double);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = d;
                    return true;
                }
                else if (type == typeof(short))
                {
                    short d = default(short);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = d;
                    return true;
                }
                else if (type == typeof(ushort))
                {
                    ushort d = default(ushort);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = d;
                    return true;
                }
                else if (type == typeof(byte))
                {
                    byte d = default(byte);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = d;
                    return true;
                }
                else if (type == typeof(sbyte))
                {
                    sbyte d = default(sbyte);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = d;
                    return true;
                }
                else if (type == typeof(char))
                {
                    char d = default(char);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = d;
                    return true;
                }
                else if (type == typeof(ulong))
                {
                    ulong d = default(ulong);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = d;
                    return true;
                }
                else if (type == typeof(long))
                {
                    long d = default(long);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = d;
                    return true;
                }
                else if (type == typeof(decimal))
                {
                    double d = default(double);
                    if (!totype(L, idx, ref d))
                        return false;
                    o = (decimal)d;
                    return true;
                }
                else
                {
                    return totype(L, idx, ref o);
                }
            }
            else if (type == typeof(string))
            {
                string s = default(string);
                if (!totype(L, idx, ref s))
                    return false;
                o = s;
                return true;
            }
            else if (type == typeof(lua_CSFunction))
            {
                lua_CSFunction f = default(lua_CSFunction);
                if (!totype(L, idx, ref f))
                    return false;
                o = f;
                return true;
            }
            IntPtr ptr = API.lua_touserdata(L, idx);
            if (ptr == IntPtr.Zero)
                return false;
            TypeInfo typeinfo;
            if (!typenames.TryGetValue(type, out typeinfo))
                return false;
            API.lua_getmetatable(L, idx);
            API.lua_rawgeti(L, -1, (int)tables.type);
            GCHandle typegch = (GCHandle)API.lua_touserdata(L, -1);
            API.lua_pop(L, 2);
            if (typegch != typeinfo.gch && !typeinfo.parents.ContainsKey((Type)typegch.Target))
                return false;
            GCHandle gch = (GCHandle)Marshal.ReadIntPtr(ptr);
            o = gch.Target;
            return true;
        }

        public static T totype<T>(IntPtr L, int idx)
        {
            T result = default(T);
            if (!totype(L, idx, ref result))
            {
                TypeInfo typeinfo;
                string name;
                if (typenames.TryGetValue(typeof(T), out typeinfo))
                {
                    name = typeinfo.name;
                }
                else
                {
                    name = "Unknow type(" + typeof(T).Name + ")";
                }
                throw new InvalidCastException(string.Format("bad argument #{0} ({1} expected, got {2})", idx, name, API.luaL_typename(L, idx)));
            }
            return result;
        }

        public static bool pushtype<T>(IntPtr L, T[] t)
        {
            if (t == null)
            {
                API.lua_pushnil(L);
                return true;
            }
            Type type = typeof(T);
            int top = API.lua_gettop(L);
            API.lua_createtable(L, t.Length, 0);
            for (int i = 0, j = t.Length; i < j; ++i)
            {
                if (!pushtype(L, t[i], type))
                {
                    API.lua_settop(L, top);
                    return false;
                }
                API.lua_rawseti(L, -2, i + 1);
            }
            return true;
        }

        public static bool pushtype(IntPtr L, int n)
        {
            API.lua_pushnumber(L, n);
            return true;
        }

        public static bool pushtype(IntPtr L, uint n)
        {
            API.lua_pushnumber(L, n);
            return true;
        }

        public static bool pushtype(IntPtr L, short n)
        {
            API.lua_pushnumber(L, n);
            return true;
        }

        public static bool pushtype(IntPtr L, ushort n)
        {
            API.lua_pushnumber(L, n);
            return true;
        }

        public static bool pushtype(IntPtr L, byte n)
        {
            API.lua_pushnumber(L, n);
            return true;
        }

        public static bool pushtype(IntPtr L, sbyte n)
        {
            API.lua_pushnumber(L, n);
            return true;
        }

        public static bool pushtype(IntPtr L, char n)
        {
            API.lua_pushnumber(L, n);
            return true;
        }

        public static bool pushtype(IntPtr L, double d)
        {
            API.lua_pushnumber(L, d);
            return true;
        }

        public static bool pushtype(IntPtr L, float f)
        {
            API.lua_pushnumber(L, f);
            return true;
        }

        public static bool pushtype(IntPtr L, bool b)
        {
            API.lua_pushboolean(L, b ? 1 : 0);
            return true;
        }

        public static bool pushtype(IntPtr L, string s)
        {
            if (s == null)
                API.lua_pushnil(L);
            else
                API.lua_pushstring(L, s);
            return true;
        }

        public static bool pushtype(IntPtr L, ulong l)
        {
            byte[] bytes = new byte[9];
            bytes[0] = 76;
            for (int i = 1; i < 9; ++i)
            {
                bytes[9 - i] = (byte)(l & 0xff);
                l >>= 8;
            }
			API.lua_pushlstring(L, bytes);
            return true;
        }

        public static bool pushtype(IntPtr L, long l)
        {
            return pushtype(L, (ulong)l);
        }

        public static bool pushtype(IntPtr L, lua_CSFunction f)
        {
            if (f == null)
                API.lua_pushnil(L);
            else
                API.lua_pushcsfunction(L, f);
            return true;
        }

        public static bool pushtype(IntPtr L, object o)
        {
            if (o == null)
            {
                API.lua_pushnil(L);
                return true;
            }
            return pushtype(L, o, o.GetType());
        }

        public static bool pushtype(IntPtr L, object o, Type type)
        {
            if (type.IsPrimitive)
            {
                if (type == typeof(bool))
                    return pushtype(L, (bool)o);
                else if (type == typeof(ulong))
                    return pushtype(L, (ulong)o);
                else if (type == typeof(long))
                    return pushtype(L, (long)o);
                else
                    return pushtype(L, Convert.ToDouble(o));
            }
            else if (type == typeof(string))
            {
                return pushtype(L, (string)o);
            }
            else if (type == typeof(lua_CSFunction))
            {
                return pushtype(L, (lua_CSFunction)o);
            }
            TypeInfo info;
            if (!typenames.TryGetValue(type, out info))
                return false;
            if (type.IsValueType)
            {
                GCHandle gch = GCHandle.Alloc(o);
                IntPtr ptr = API.lua_newuserdata(L, Marshal.SizeOf(typeof(IntPtr)));
                Marshal.WriteIntPtr(ptr, (IntPtr)gch);
            }
            else
            {
                GCHandle gch;
                if (refptrs.TryGetValue(o, out gch))
                {
                    getweaktable(L);
                    API.lua_pushlightuserdata(L, (IntPtr)gch);
                    API.lua_gettable(L, -2);
                    API.lua_replace(L, -2);
                    return true;
                }
                gch = GCHandle.Alloc(o);
                refptrs.Add(o, gch);
                IntPtr ptr = API.lua_newuserdata(L, Marshal.SizeOf(typeof(IntPtr)));
                Marshal.WriteIntPtr(ptr, (IntPtr)gch);
                getweaktable(L);
                API.lua_pushlightuserdata(L, (IntPtr)gch);
                API.lua_pushvalue(L, -3);
                API.lua_settable(L, -3);
                API.lua_pop(L, 1);
            }
            API.lua_pushlightuserdata(L, (IntPtr)info.gch);
            API.lua_gettable(L, Consts.LUA_REGISTRYINDEX);
            API.lua_setmetatable(L, -2);
            return true;
        }

        public static void restore<T>(IntPtr L, int idx, ref T t)
        {
            if (typeof(T).IsValueType)
            {
                IntPtr ptr = API.lua_touserdata(L, idx);
                if (ptr != IntPtr.Zero)
                {
                    GCHandle gch = (GCHandle)Marshal.ReadIntPtr(ptr);
                    gch.Target = t;
                }
            }
        }

        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __type(IntPtr L)
        {
            API.lua_pushvalue(L, API.lua_upvalueindex(1));
            return 1;
        }

        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __index(IntPtr L)
        {
            API.lua_settop(L, 2);
            API.lua_pushvalue(L, 2);
            API.lua_gettable(L, API.lua_upvalueindex(1));
            if (!API.lua_isnil(L, -1))
            {
                API.lua_replace(L, 2);
                API.lua_insert(L, 1);
                API.lua_call(L, 1, Consts.LUA_MULTRET);
                return API.lua_gettop(L);
            }
            API.lua_pop(L, 1);
            API.lua_rawget(L, API.lua_upvalueindex(2));
            return 1;
        }

        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __newindex(IntPtr L)
        {
            API.lua_settop(L, 3);
            API.lua_insert(L, 2);
            API.lua_pushvalue(L, 3);
            API.lua_gettable(L, API.lua_upvalueindex(1));
            if (!API.lua_isnil(L, -1))
            {
                API.lua_replace(L, 3);
                API.lua_insert(L, 1);
                API.lua_call(L, 2, Consts.LUA_MULTRET);
                return 0;
            }
            API.lua_pop(L, 1);
            API.luaL_error(L, "bad field '{0}'", API.luaL_checkstring(L, 3));
            return 0;
        }

        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __value(IntPtr L)
        {
            API.lua_settop(L, 2);
            API.lua_pushvalue(L, 2);
            API.lua_gettable(L, API.lua_upvalueindex(1));
            if (!API.lua_isnil(L, -1))
            {
                API.lua_replace(L, 1);
                API.lua_settop(L, 1);
                API.lua_call(L, 0, Consts.LUA_MULTRET);
                return API.lua_gettop(L);
            }
            return 1;
        }

        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __newvalue(IntPtr L)
        {
            API.lua_settop(L, 3);
            API.lua_insert(L, 2);
            API.lua_pushvalue(L, 3);
            API.lua_gettable(L, API.lua_upvalueindex(1));
            if (!API.lua_isnil(L, -1))
            {
                API.lua_replace(L, 1);
                API.lua_settop(L, 2);
                API.lua_call(L, 1, Consts.LUA_MULTRET);
                return 0;
            }
            API.lua_pop(L, 1);
            API.lua_insert(L, 2);
            API.lua_rawset(L, API.lua_upvalueindex(2));
            return 0;
        }

        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __gc(IntPtr L)
        {
            IntPtr ptr = API.lua_touserdata(L, 1);
            GCHandle gch = (GCHandle)Marshal.ReadIntPtr(ptr);
            GCHandle value;
            if (refptrs.TryGetValue(gch.Target, out value))
            {
                if (value != gch)
                {
                }
                refptrs.Remove(gch.Target);
            }
            gch.Free();
            return 0;
        }
    }
}*/