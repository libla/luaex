using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace CodeDom
{
	internal static class Utils
	{
		private static Dictionary<Type, string> predefinetype = null;

		private static string predefinetypename(Type type)
		{
			if (predefinetype == null)
			{
				predefinetype = new Dictionary<Type, string>
				{
					{typeof(void), "void"},
					{typeof(string), "string"},
					{typeof(object), "object"},
					{typeof(int), "int"},
					{typeof(uint), "uint"},
					{typeof(byte), "byte"},
					{typeof(sbyte), "sbyte"},
					{typeof(char), "char"},
					{typeof(short), "short"},
					{typeof(ushort), "ushort"},
					{typeof(long), "long"},
					{typeof(ulong), "ulong"},
					{typeof(float), "float"},
					{typeof(double), "double"},
					{typeof(decimal), "decimal"}
				};
			}
			string name;
			if (predefinetype.TryGetValue(type, out name))
			{
				return name;
			}
			return null;
		}

		public static string Name(Type type, IDictionary<string, bool> namespaces)
		{
			List<string> arrays = new List<string>();
			while (type.IsArray)
			{
				type = type.GetElementType();
				arrays.Add("[]");
			}
			string name = predefinetypename(type);
			if (name != null)
				return name + string.Concat(arrays.ToArray());
			if (type.IsGenericType)
			{
				if (type.IsGenericTypeDefinition)
				{
					name = type.Name;
					name = name.Substring(0, name.IndexOf("`", StringComparison.Ordinal));
				}
				else
				{
					name = type.GetGenericTypeDefinition().Name;
					name = name.Substring(0, name.IndexOf("`", StringComparison.Ordinal));
					List<string> args = new List<string>();
					foreach (Type t in type.GetGenericArguments())
					{
						args.Add(Name(t, namespaces));
					}
					name = name + "<" + string.Join(", ", args.ToArray()) + ">";
				}
			}
			else
			{
				name = type.Name;
			}
			name = name + string.Concat(arrays.ToArray());
			if (type.DeclaringType == null)
			{
				string nsname = type.Namespace;
				if (!string.IsNullOrEmpty(nsname) && namespaces != null && namespaces.ContainsKey(nsname))
					nsname = "";
				if (string.IsNullOrEmpty(nsname))
					return name;
				return nsname + "." + name;
			}
			return Name(type.DeclaringType, namespaces) + "." + name;
		}

		public static string Name(Type type)
		{
			return Name(type, null);
		}

		public static string Name(CodeTypeDefine type)
		{
			if (type.parenttype != null)
			{
				return Name(type.parenttype) + "." + type.name;
			}
			else if (type.parentns != null)
			{
				return Name(type.parentns) + "." + type.name;
			}
			else
			{
				return type.name;
			}
		}

		public static string Name(CodeTypeDefine type, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			CodeTypeDefine parent = ctype;
			while (parent != null)
			{
				if (parent == type)
					return "";
				parent = parent.parenttype;
			}
			string prefix = "";
			if (type.parenttype != null)
			{
				prefix = Name(type.parenttype, ctype, namespaces);
			}
			else if (type.parentns != null)
			{
				prefix = Name(type.parentns, namespaces);
			}
			return string.IsNullOrEmpty(prefix) ? type.name : prefix + "." + type.name;
		}

		public static string Name(CodePackage ns)
		{
			LinkedList<string> namelist = new LinkedList<string>();
			namelist.AddFirst(ns.name);
			CodePackage parent = ns.parent;
			while (parent != null)
			{
				namelist.AddFirst(parent.name);
				parent = parent.parent;
			}
			string[] names = new string[namelist.Count];
			namelist.CopyTo(names, 0);
			namelist.Clear();
			return string.Join(".", names);
		}

		public static string Name(CodePackage ns, IDictionary<string, bool> namespaces)
		{
			LinkedList<string> namelist = new LinkedList<string>();
			namelist.AddFirst(ns.name);
			CodePackage parent = ns.parent;
			while (parent != null)
			{
				namelist.AddFirst(parent.name);
				parent = parent.parent;
			}
			string[] names = new string[namelist.Count];
			namelist.CopyTo(names, 0);
			namelist.Clear();
			string result = string.Join(".", names);
			if (namespaces.ContainsKey(result))
				result = "";
			return result;
		}

		public static void Build(string file)
		{
			List<string> allassemblys = new List<string>();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				string name = assembly.GetName().Name;
				if (assembly.EntryPoint == null)
					name = name + ".dll";
				else
					name = name + ".exe";
				allassemblys.Add(name);
			}
			CSharpCodeProvider cprovider = new CSharpCodeProvider();
			CompilerParameters cp = new CompilerParameters(allassemblys.ToArray())
			{
				GenerateExecutable = false,
				IncludeDebugInformation = false,
				GenerateInMemory = false,
				OutputAssembly = file.Substring(0, file.LastIndexOf(".", StringComparison.InvariantCulture)) + ".dll",
			};
			CompilerResults cr = cprovider.CompileAssemblyFromFile(cp, file);
			if (cr.Errors.Count > 0)
			{
				for (int i = 0; i < cr.Errors.Count; i++)
					Console.WriteLine(cr.Errors[i].ToString());
			}
		}

		public static void Build(byte[] source)
		{
			CSharpCodeProvider cprovider = new CSharpCodeProvider();
			CompilerParameters cp = new CompilerParameters(new string[] {"System.dll"})
			{
				GenerateExecutable = false,
				IncludeDebugInformation = false,
				GenerateInMemory = false,
			};
			CompilerResults cr = cprovider.CompileAssemblyFromSource(cp, System.Text.Encoding.Default.GetString(source));
			if (cr.Errors.Count > 0)
			{
				for (int i = 0; i < cr.Output.Count; i++)
					Console.WriteLine(cr.Output[i]);
				for (int i = 0; i < cr.Errors.Count; i++)
					Console.WriteLine(i.ToString() + ": " + cr.Errors[i].ToString());
			}
		}
	}

	public class CodeUnit
	{
		public CodeUnit()
		{
			_namespaces = new List<CodePackage>();
			_imports = new List<string>();
			_types = new List<CodeTypeDefine>();
		}

		public void Import(string ns)
		{
			_imports.Add(ns);
		}

		public void Add(CodePackage ns)
		{
			_namespaces.Add(ns);
		}

		public void Add(CodeTypeDefine type)
		{
			_types.Add(type);
			type.parentns = null;
			type.parenttype = null;
		}

		public CodeComment comments;
		protected readonly List<CodePackage> _namespaces;
		protected readonly List<string> _imports;
		protected readonly List<CodeTypeDefine> _types;

		public void Compile(TextWriter writer)
		{
			if (comments != null)
				comments.Compile(writer, "");
			Dictionary<string, bool> namespaces = new Dictionary<string, bool>();
			foreach (string import in _imports)
			{
				if (!namespaces.ContainsKey(import))
					namespaces.Add(import, true);
			}
			foreach (string ns in _imports)
			{
				writer.WriteLine("using " + ns + ";");
			}
			if (_types.Count > 0)
				writer.WriteLine();
			foreach (CodeTypeDefine type in _types)
			{
				type.Compile(writer, "", null, namespaces);
			}
			if (_namespaces.Count > 0)
				writer.WriteLine();
			foreach (CodePackage ns in _namespaces)
			{
				ns.Compile(writer, "", null, namespaces);
			}
		}
	}

	public class CodePackage
	{
		public CodePackage(string s)
		{
			name = s;
			_namespaces = new List<CodePackage>();
			_imports = new List<string>();
			_types = new List<CodeTypeDefine>();
		}

		public void Import(string ns)
		{
			_imports.Add(ns);
		}

		public void Add(CodePackage ns)
		{
			_namespaces.Add(ns);
			ns.parent = this;
		}

		public void Add(CodeTypeDefine type)
		{
			_types.Add(type);
			type.parentns = this;
			type.parenttype = null;
		}

		public string name;
		public CodeComment comments;
		public CodePackage parent;
		protected readonly List<CodePackage> _namespaces;
		protected readonly List<string> _imports;
		protected readonly List<CodeTypeDefine> _types;

		public void Compile(TextWriter writer, string indent, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			if (comments != null)
				comments.Compile(writer, indent);
			Dictionary<string, bool> newnamespaces = new Dictionary<string, bool>(namespaces);
			string fullname = Utils.Name(this);
			if (!newnamespaces.ContainsKey(fullname))
				newnamespaces.Add(fullname, true);
			foreach (string import in _imports)
			{
				if (!newnamespaces.ContainsKey(import))
					newnamespaces.Add(import, true);
			}
			writer.Write(indent);
			writer.WriteLine("namespace {0}", name);
			writer.Write(indent);
			writer.WriteLine("{");
			string newindent = indent + "\t";
			foreach (string ns in _imports)
			{
				writer.Write(newindent);
				writer.WriteLine("using " + ns + ";");
			}
			foreach (CodeTypeDefine type in _types)
			{
				type.Compile(writer, newindent, ctype, newnamespaces);
			}
			if (_namespaces.Count > 0)
				writer.WriteLine();
			foreach (CodePackage ns in _namespaces)
			{
				ns.Compile(writer, newindent, ctype, newnamespaces);
			}
			writer.Write(indent);
			writer.WriteLine("}");
		}
	}

	public abstract class CodeTypeDefine
	{
		protected CodeTypeDefine(string s)
		{
			name = s;
			modify = "public";
			_attributes = new List<string>();
		}

		public List<string> attributes
		{
			get { return _attributes; }
		}

		public string name;
		public CodePackage parentns;
		public CodeStructDefine parenttype;
		public CodeComment comments;
		public string modify;
		protected readonly List<string> _attributes;

		public void Compile(TextWriter writer, string indent, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			if (comments != null)
				comments.Compile(writer, indent);
			foreach (string attribute in _attributes)
			{
				writer.Write(indent);
				writer.WriteLine("[{0}]", attribute);
			}
			writer.Write(indent);
			if (!string.IsNullOrEmpty(modify))
			{
				writer.Write(modify);
				writer.Write(" ");
			}
			CompileType(writer, indent, ctype, namespaces);
		}

		public abstract void CompileType(TextWriter writer, string indent, CodeTypeDefine ctype,
										IDictionary<string, bool> namespaces);
	}

	public class CodeStructDefine : CodeTypeDefine
	{
		public CodeStructDefine(string s) : base(s)
		{
			_types = new List<CodeTypeDefine>();
			_members = new List<CodeTypeMember>();
		}

		public void Add(CodeTypeDefine type)
		{
			_types.Add(type);
			type.parentns = parentns;
			type.parenttype = this;
		}

		public void Add(CodeTypeMember member)
		{
			_members.Add(member);
		}

		protected readonly List<CodeTypeDefine> _types;
		protected readonly List<CodeTypeMember> _members;

		public virtual void CompileSelf(TextWriter writer, string indent, CodeTypeDefine ctype,
										IDictionary<string, bool> namespaces)
		{
			writer.WriteLine("struct {0}", name);
			writer.Write(indent);
			writer.WriteLine("{");
		}

		public override void CompileType(TextWriter writer, string indent, CodeTypeDefine ctype,
										IDictionary<string, bool> namespaces)
		{
			CompileSelf(writer, indent, ctype, namespaces);
			string newindent = indent + "\t";
			//_members.Sort();
			foreach (CodeTypeMember member in _members)
			{
				member.Compile(writer, newindent, this, namespaces);
			}
			if (_types.Count > 0)
				writer.WriteLine();
			foreach (CodeTypeDefine type in _types)
			{
				type.Compile(writer, newindent, this, namespaces);
			}
			writer.Write(indent);
			writer.WriteLine("}");
		}
	}

	public class CodeClassDefine : CodeStructDefine
	{
		public CodeClassDefine(string s) : base(s) {}

		public CodeMethod finalize;

		public override void CompileSelf(TextWriter writer, string indent, CodeTypeDefine ctype,
										IDictionary<string, bool> namespaces)
		{
			writer.WriteLine("class {0}", name);
			writer.Write(indent);
			writer.WriteLine("{");
			if (finalize != null)
			{
				string newindent = indent + "\t";
				writer.Write(newindent);
				writer.WriteLine("~{0}()", name);
				finalize.CompileBody(writer, newindent, this, namespaces);
			}
		}
	}

	public class CodeDelegateDefine : CodeTypeDefine
	{
		public CodeDelegateDefine(string s) : base(s)
		{
			invoke = new CodeMethod();
		}

		protected readonly CodeMethod invoke;

		public override void CompileType(TextWriter writer, string indent, CodeTypeDefine ctype,
										IDictionary<string, bool> namespaces)
		{
			writer.Write("delegate ");
			invoke.CompileReturn(writer, indent, ctype, namespaces);
			writer.Write(" {0}", name);
			invoke.CompileParam(writer, indent, ctype, namespaces);
			writer.WriteLine();
		}
	}

	public class CodeEnumDefine : CodeTypeDefine
	{
		public CodeEnumDefine(string s) : base(s)
		{
			items = new List<string>();
			values = new List<object>();
		}

		public void SetType(string s)
		{
			type = s;
		}

		public void SetType(Type t)
		{
			if (!t.IsPrimitive)
			{
				throw new ArgumentException();
			}
			type = Utils.Name(t);
		}

		public void AddItem(string item)
		{
			AddItem(item, null);
		}

		public void AddItem(string item, object value)
		{
			items.Add(item);
			values.Add(value);
		}

		protected string type;
		protected readonly List<string> items;
		protected readonly List<object> values;

		public override void CompileType(TextWriter writer, string indent, CodeTypeDefine ctype,
										IDictionary<string, bool> namespaces)
		{
			writer.Write("enum {0}", name);
			if (!string.IsNullOrEmpty(type))
			{
				writer.Write(" : {0}", type);
			}
			writer.WriteLine();
			writer.Write(indent);
			writer.WriteLine("{");
			for (int i = 0; i < items.Count; ++i)
			{
				string item = items[i];
				object value = i < values.Count ? values[i] : null;
				if (value != null)
				{
					writer.WriteLine("{0}\t{1} = {2},", indent, item, value);
				}
				else
				{
					writer.WriteLine("{0}\t{1},", indent, item);
				}
			}
			writer.Write(indent);
			writer.WriteLine("}");
		}
	}

	public class CodeVariable
	{
		public CodeVariable(CodeTypeExp type, string name) : this(type, name, null) {}

		public CodeVariable(CodeTypeExp type, string name, CodeExp initexp)
		{
			_type = type;
			_name = name;
			_init = initexp;
		}

		public string name
		{
			get { return _name; }
		}

		public CodeExp init
		{
			get { return _init; }
			set { _init = value; }
		}

		protected readonly CodeTypeExp _type;
		protected readonly string _name;
		protected CodeExp _init;

		public void Compile(TextWriter writer, string indent, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			_type.Compile(writer, indent, ctype, namespaces);
			writer.Write(" {0}", _name);
			if (_init != null)
			{
				writer.Write(" = ");
				_init.Compile(writer, indent, ctype, namespaces);
			}
		}
	}

	public class CodeParam
	{
		public enum ParamMode
		{
			IN,
			REF,
			OUT,
			PARAMS,
		}

		public CodeParam(CodeTypeExp type, string name) : this(type, name, ParamMode.IN) {}

		public CodeParam(CodeTypeExp type, string name, ParamMode mode)
		{
			_type = type;
			_name = name;
			_mode = mode;
		}

		public static string ToString(ParamMode mode)
		{
			switch (mode)
			{
			case ParamMode.REF:
				return "ref";
			case ParamMode.OUT:
				return "out";
			case ParamMode.PARAMS:
				return "params";
			default:
				return "";
			}
		}

		public CodeTypeExp type
		{
			get { return _type; }
		}

		public string mode
		{
			get { return ToString(_mode); }
		}

		public string name
		{
			get { return _name; }
		}

		protected readonly CodeTypeExp _type;
		protected readonly string _name;
		protected readonly ParamMode _mode;

		public void Compile(TextWriter writer, string indent, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			string mode = ToString(_mode);
			if (mode != "")
			{
				writer.Write("{0} ", mode);
			}
			_type.Compile(writer, indent, ctype, namespaces);
			writer.Write(" {0}", _name);
		}
	}

	public class CodeComment
	{
		public CodeComment()
		{
			comments = new List<string>();
		}

		public void Add(string comment)
		{
			comments.Add(comment);
		}

		protected readonly List<string> comments;

		public void Compile(TextWriter writer, string indent)
		{
			if (comments.Count == 1)
			{
				writer.Write(indent);
				writer.WriteLine("// {0}", comments[0]);
			}
			else if (comments.Count > 0)
			{
				writer.Write(indent);
				writer.WriteLine("/*");
				foreach (string comment in comments)
				{
					writer.Write(indent);
					writer.WriteLine(" * {0}", comment);
				}
				writer.Write(indent);
				writer.WriteLine(" */");
			}
		}
	}

	public class CodeMethod
	{
		public CodeMethod()
		{
			returntype = new CodeTypeExp(typeof(void));
			paramlist = new List<CodeParam>();
			blockstat = new CodeBlockStat();
		}

		public void Return(CodeTypeExp type)
		{
			returntype = type;
		}

		public List<CodeParam> param
		{
			get { return paramlist; }
		}

		public CodeBlockStat block
		{
			get { return blockstat; }
		}

		protected CodeTypeExp returntype;
		protected readonly List<CodeParam> paramlist;
		protected readonly CodeBlockStat blockstat;

		public void CompileDecl(TextWriter writer, string name, string indent, CodeTypeDefine ctype,
								IDictionary<string, bool> namespaces)
		{
			CompileReturn(writer, indent, ctype, namespaces);
			writer.Write(" {0}", name);
			CompileParam(writer, indent, ctype, namespaces);
			writer.WriteLine();
		}

		public void CompileReturn(TextWriter writer, string indent, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			returntype.Compile(writer, indent, ctype, namespaces);
		}

		public void CompileParam(TextWriter writer, string indent, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			writer.Write("(");
			for (int i = 0; i < paramlist.Count; ++i)
			{
				if (i != 0)
				{
					writer.Write(", ");
				}
				paramlist[i].Compile(writer, indent, ctype, namespaces);
			}
			writer.Write(")");
		}

		public void CompileBody(TextWriter writer, string indent, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			blockstat.PreCompile(ctype, null, 0);
			if (blockstat.stats.Count == 1)
			{
				writer.Write(indent);
				writer.WriteLine("{");
				blockstat.Compile(writer, indent, ctype, namespaces);
				writer.Write(indent);
				writer.WriteLine("}");
			}
			else
			{
				blockstat.Compile(writer, indent, ctype, namespaces);
			}
		}
	}

	public abstract class CodeTypeMember
	{
		protected CodeTypeMember()
		{
			_attributes = new List<string>();
		}

		public List<string> attributes
		{
			get { return _attributes; }
		}

		public CodeComment comments;
		public string modify;
		protected readonly List<string> _attributes;

		public void Compile(TextWriter writer, string indent, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			if (comments != null)
				comments.Compile(writer, indent);
			foreach (string attribute in _attributes)
			{
				writer.Write(indent);
				writer.WriteLine("[{0}]", attribute);
			}
			writer.Write(indent);
			if (!string.IsNullOrEmpty(modify))
			{
				writer.Write(modify);
				writer.Write(" ");
			}
			CompileMember(writer, indent, ctype, namespaces);
		}

		public abstract void CompileMember(TextWriter writer, string indent, CodeTypeDefine ctype,
											IDictionary<string, bool> namespaces);
	}

	public class CodeTypeField : CodeTypeMember
	{
		public CodeTypeField(CodeTypeExp type, string name) : this(type, name, null) {}

		public CodeTypeField(CodeTypeExp type, string name, CodeExp initexp)
		{
			var = new CodeVariable(type, name, initexp);
		}

		public CodeExp init
		{
			get { return var.init; }
			set { var.init = value; }
		}

		public string name
		{
			get { return var.name; }
		}

		protected readonly CodeVariable var;

		public override void CompileMember(TextWriter writer, string indent, CodeTypeDefine ctype,
											IDictionary<string, bool> namespaces)
		{
			var.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(";");
		}
	}

	public class CodeTypeProperty : CodeTypeMember
	{
		public CodeTypeProperty(CodeTypeExp type, string name)
		{
			_type = type;
			_name = name;
		}

		public string name
		{
			get { return _name; }
		}

		public CodeBlockStat getter;
		public CodeBlockStat setter;
		protected readonly CodeTypeExp _type;
		protected readonly string _name;

		public override void CompileMember(TextWriter writer, string indent, CodeTypeDefine ctype,
											IDictionary<string, bool> namespaces)
		{
			_type.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(" {0}", _name);
			writer.Write(indent);
			writer.WriteLine("{");
			string newindent = indent + "\t";
			if (getter != null)
			{
				if (getter.stats.Count == 0)
				{
					writer.Write(newindent);
					writer.WriteLine("get {}");
				}
				else if (getter.stats.Count == 1)
				{
					writer.Write(newindent);
					writer.Write("get {");
					getter.Compile(writer, newindent, ctype, namespaces);
					writer.WriteLine("}");
				}
				else
				{
					writer.Write(newindent);
					writer.WriteLine("get");
					getter.Compile(writer, newindent, ctype, namespaces);
				}
			}
			if (setter != null)
			{
				if (setter.stats.Count == 0)
				{
					writer.Write(newindent);
					writer.WriteLine("set {}");
				}
				else if (setter.stats.Count == 1)
				{
					writer.Write(newindent);
					writer.Write("set {");
					setter.Compile(writer, newindent, ctype, namespaces);
					writer.WriteLine("}");
				}
				else
				{
					writer.Write(newindent);
					writer.WriteLine("set");
					setter.Compile(writer, newindent, ctype, namespaces);
				}
			}
			writer.Write(indent);
			writer.WriteLine("}");
		}
	}

	public class CodeTypeMethod : CodeTypeMember
	{
		public CodeTypeMethod(string name)
		{
			_name = name;
			_method = new CodeMethod();
		}

		public string name
		{
			get { return _name; }
		}

		public CodeMethod method
		{
			get { return _method; }
		}

		protected readonly string _name;
		protected readonly CodeMethod _method;

		public override void CompileMember(TextWriter writer, string indent, CodeTypeDefine ctype,
											IDictionary<string, bool> namespaces)
		{
			_method.CompileDecl(writer, _name, indent, ctype, namespaces);
			_method.CompileBody(writer, indent, ctype, namespaces);
		}
	}

	public class CodeTypeNew : CodeTypeMember
	{
		public CodeTypeNew()
		{
			_method = new CodeMethod();
		}

		public CodeMethod method
		{
			get { return _method; }
		}

		protected readonly CodeMethod _method;

		public override void CompileMember(TextWriter writer, string indent, CodeTypeDefine ctype,
											IDictionary<string, bool> namespaces)
		{
			writer.Write(ctype.name);
			_method.CompileParam(writer, indent, ctype, namespaces);
			writer.WriteLine();
			_method.CompileBody(writer, indent, ctype, namespaces);
		}
	}

	public abstract class CodeExp
	{
		public virtual void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num) {}

		public abstract void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces);
	}

	public class CodeTextExp : CodeExp
	{
		public CodeTextExp(string s)
		{
			_text = s;
		}

		public string text
		{
			set { _text = value; }
		}

		protected string _text;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			if (_text != null)
			{
				writer.Write(_text);
			}
		}
	}

	public class CodeTypeExp : CodeExp
	{
		public CodeTypeExp(CodeTypeDefine type)
		{
			strtype = null;
			typetype = null;
			definetype = type;
		}

		public CodeTypeExp(Type type)
		{
			strtype = null;
			typetype = type;
			definetype = null;
		}

		public CodeTypeExp(string type)
		{
			strtype = type;
			typetype = null;
			definetype = null;
		}

		public CodeTypeExp(CodeTypeExp rhs)
		{
			strtype = rhs.strtype;
			typetype = rhs.typetype;
			definetype = rhs.definetype;
		}

		public static implicit operator CodeTypeExp(CodeTypeDefine type)
		{
			return new CodeTypeExp(type);
		}

		public static implicit operator CodeTypeExp(Type type)
		{
			return new CodeTypeExp(type);
		}

		public static implicit operator CodeTypeExp(string type)
		{
			return new CodeTypeExp(type);
		}

		public virtual string name
		{
			get
			{
				if (strtype != null)
				{
					return strtype;
				}
				if (typetype != null)
				{
					if (typetype.IsGenericType && typetype.IsGenericTypeDefinition)
						throw new NotSupportedException(typetype.FullName);
					return Utils.Name(typetype);
				}
				return Utils.Name(definetype);
			}
		}

		protected readonly string strtype;
		protected readonly Type typetype;
		protected readonly CodeTypeDefine definetype;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			string name;
			if (strtype != null)
			{
				name = strtype;
			}
			else if (typetype != null)
			{
				if (typetype.IsGenericType && typetype.IsGenericTypeDefinition)
					throw new NotSupportedException(typetype.FullName);
				name = Utils.Name(typetype, namespaces);
			}
			else
			{
				name = Utils.Name(definetype, ctype, namespaces);
				if (name == "")
					name = definetype.name;
			}
			writer.Write(name);
		}
	}

	public class CodeTypeArrayExp : CodeTypeExp
	{
		public CodeTypeArrayExp(CodeTypeExp exp)
			: base(exp) { }

		public override string name
		{
			get
			{
				return base.name + "[]";
			}
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			base.Compile(writer, indent, ctype, namespaces);
			writer.Write("[]");
		}
	}

	public class CodeTypeGenericExp : CodeTypeExp
	{
		protected readonly CodeTypeExp arg;
		protected readonly CodeTypeExp[] args;

		public CodeTypeGenericExp(Type type, CodeTypeExp arg, params CodeTypeExp[] args)
			: base(type)
		{
			this.arg = arg;
			this.args = args;
		}

		public override string name
		{
			get
			{
				List<string> nameargs = new List<string>(args.Length + 1) { arg.name };
				foreach (CodeTypeExp t in args)
				{
					nameargs.Add(t.name);
				}
				return Utils.Name(typetype) + "<" + string.Join(", ", nameargs.ToArray()) + ">";
			}
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(Utils.Name(typetype, namespaces));
			writer.Write("<");
			arg.Compile(writer, indent, ctype, namespaces);
			foreach (CodeTypeExp t in args)
			{
				writer.Write(", ");
				t.Compile(writer, indent, ctype, namespaces);
			}
			writer.Write(">");
		}
	}

	public class CodeCastExp : CodeExp
	{
		public CodeCastExp(CodeTypeExp type, CodeExp exp)
		{
			_type = type;
			_exp = exp;
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write("(");
			_type.Compile(writer, indent, ctype, namespaces);
			writer.Write(")");
			writer.Write("(");
			_exp.Compile(writer, indent, ctype, namespaces);
			writer.Write(")");
		}

		protected readonly CodeTypeExp _type;
		protected readonly CodeExp _exp;
	}

	public class CodeLiteralExp : CodeExp
	{
		public CodeLiteralExp(object o)
		{
			literal = o;
		}

		protected readonly object literal;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			if (literal == null)
			{
				writer.Write("null");
			}
			else if (literal is string)
			{
				string quote = (string) literal;
				quote = quote.Replace("\"", "\\\"");
				quote = quote.Replace("\\", "\\\\");
				quote = quote.Replace("\r", "\\r");
				quote = quote.Replace("\n", "\\n");
				quote = quote.Replace("\t", "\\t");
				quote = quote.Replace("\v", "\\v");
				writer.Write("\"{0}\"", quote);
			}
			else if (literal is bool)
			{
				if ((bool)literal)
					writer.Write("true");
				else
					writer.Write("false");
			}
			else
			{
				writer.Write(literal.ToString());
			}
		}
	}

	public class CodeTypeOfExp : CodeExp
	{
		public CodeTypeOfExp(CodeTypeExp type)
		{
			_type = type;
		}

		protected readonly CodeTypeExp _type;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write("typeof(");
			_type.Compile(writer, indent, ctype, namespaces);
			writer.Write(")");
		}
	}

	public class CodeDefaultValueExp : CodeExp
	{
		public CodeDefaultValueExp(CodeTypeExp type)
		{
			_type = type;
		}

		protected readonly CodeTypeExp _type;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write("default(");
			_type.Compile(writer, indent, ctype, namespaces);
			writer.Write(")");
		}
	}

	public class CodeBaseTypeExp : CodeExp
	{
		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write("base");
		}
	}

	public class CodeThisExp : CodeExp
	{
		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write("this");
		}
	}

	public class CodeIndexExp : CodeExp
	{
		public CodeIndexExp(CodeExp exp, CodeExp index)
		{
			_exp = exp;
			_index = index;
		}

		protected readonly CodeExp _exp;
		protected readonly CodeExp _index;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write("(");
			_exp.Compile(writer, indent, ctype, namespaces);
			writer.Write(")[");
			_index.Compile(writer, indent, ctype, namespaces);
			writer.Write("]");
		}
	}

	public class CodeVariableExp : CodeExp
	{
		public CodeVariableExp(CodeVariable var)
		{
			_var = var;
		}

		protected readonly CodeVariable _var;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			if (!block.Contain(_var))
			{
				block.values.Add(_var, num);
			}
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(_var.name);
		}
	}

	public class CodeVariableDefineExp : CodeExp
	{
		public CodeVariableDefineExp(CodeVariable var)
		{
			_var = var;
		}

		protected readonly CodeVariable _var;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			block.hasvalues.Add(_var, true);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			_var.Compile(writer, indent, ctype, namespaces);
		}
	}

	public class CodeParamExp : CodeExp
	{
		public CodeParamExp(string s) : this(s, CodeParam.ParamMode.IN) {}

		public CodeParamExp(string s, CodeParam.ParamMode m)
		{
			name = s;
			mode = m;
		}

		protected readonly string name;
		protected readonly CodeParam.ParamMode mode;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			string mode = CodeParam.ToString(this.mode);
			if (mode != "")
			{
				writer.Write("{0} ", mode);
			}
			writer.Write(name);
		}
	}

	public class CodeMemberExp : CodeExp
	{
		public CodeMemberExp(CodeExp exp, string field)
		{
			_exp = exp;
			_field = field;
		}

		protected readonly CodeExp _exp;
		protected readonly string _field;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			_exp.Compile(writer, indent, ctype, namespaces);
			writer.Write(".{0}", _field);
		}
	}

	public class CodeThisMemberExp : CodeExp
	{
		public CodeThisMemberExp(string field)
		{
			_field = field;
		}

		protected readonly string _field;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(_field);
		}
	}

	public class CodeAssignExp : CodeExp
	{
		public CodeAssignExp(CodeExp lexp, CodeExp rexp)
		{
			_lexp = lexp;
			_rexp = rexp;
		}

		protected readonly CodeExp _lexp;
		protected readonly CodeExp _rexp;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			_lexp.Compile(writer, indent, ctype, namespaces);
			writer.Write(" = ");
			_rexp.Compile(writer, indent, ctype, namespaces);
		}
	}

	public class CodeArrayInitExp : CodeExp
	{
		public CodeArrayInitExp(CodeTypeExp type)
		{
			_type = type;
			_list = new List<CodeExp>();
		}

		protected readonly CodeExp _type;
		protected readonly List<CodeExp> _list;

		public List<CodeExp> list
		{
			get { return _list; }
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write("new ");
			_type.Compile(writer, indent, ctype, namespaces);
			writer.Write("[] {");
			if (_list.Count > 0)
			{
				writer.Write(" ");
				_list[0].Compile(writer, indent, ctype, namespaces);
				for (int i = 1; i < _list.Count; i++)
				{
					writer.Write(", ");
					_list[i].Compile(writer, indent, ctype, namespaces);
				}
			}
			writer.Write(" }");
		}
	}

	public abstract class CodeMethodInvokeExp : CodeExp
	{
		protected CodeMethodInvokeExp()
		{
			_param = new List<CodeExp>();
			_parammode = new Dictionary<CodeExp, CodeParam.ParamMode>();
			_templates = new List<CodeTypeExp>();
		}

		public List<CodeExp> param
		{
			get { return _param; }
		}

		public Dictionary<CodeExp, CodeParam.ParamMode> parammode
		{
			get { return _parammode; }
		}

		public List<CodeTypeExp> templates
		{
			get { return _templates; }
		}

		protected readonly List<CodeExp> _param;
		protected readonly Dictionary<CodeExp, CodeParam.ParamMode> _parammode;
		protected readonly List<CodeTypeExp> _templates;

		public void CompileTemplate(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			if (_templates.Count > 0)
			{
				writer.Write("<");
				for (int i = 0; i < _templates.Count; ++i)
				{
					if (i != 0)
					{
						writer.Write(", ");
					}
					_templates[i].Compile(writer, indent, ctype, namespaces);
				}
				writer.Write(">");
			}
		}

		public void CompileParam(TextWriter writer, string indent, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			writer.Write("(");
			for (int i = 0; i < _param.Count; ++i)
			{
				if (i != 0)
				{
					writer.Write(", ");
				}
				CodeParam.ParamMode m;
				if (_parammode.TryGetValue(_param[i], out m))
				{
					string mode = CodeParam.ToString(m);
					if (mode != "")
					{
						writer.Write("{0} ", mode);
					}
				}
				_param[i].Compile(writer, indent, ctype, namespaces);
			}
			writer.Write(")");
		}
	}

	public class CodeDelegateInvokeExp : CodeMethodInvokeExp
	{
		public CodeDelegateInvokeExp(CodeExp exp)
		{
			_exp = exp;
		}

		protected readonly CodeExp _exp;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			_exp.Compile(writer, indent, ctype, namespaces);
			CompileTemplate(writer, indent, ctype, namespaces);
			CompileParam(writer, indent, ctype, namespaces);
		}
	}

	public class CodeTypeMethodInvokeExp : CodeMethodInvokeExp
	{
		public CodeTypeMethodInvokeExp(CodeExp exp, string field)
		{
			_exp = new CodeMemberExp(exp, field);
		}

		protected readonly CodeMemberExp _exp;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			_exp.Compile(writer, indent, ctype, namespaces);
			CompileTemplate(writer, indent, ctype, namespaces);
			CompileParam(writer, indent, ctype, namespaces);
		}
	}

	public class CodeThisTypeMethodInvokeExp : CodeMethodInvokeExp
	{
		public CodeThisTypeMethodInvokeExp(string field)
		{
			_exp = new CodeThisMemberExp(field);
		}

		protected readonly CodeThisMemberExp _exp;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			_exp.Compile(writer, indent, ctype, namespaces);
			CompileTemplate(writer, indent, ctype, namespaces);
			CompileParam(writer, indent, ctype, namespaces);
		}
	}

	public class CodeNewExp : CodeMethodInvokeExp
	{
		public CodeNewExp(CodeTypeExp exp)
		{
			_exp = exp;
		}

		protected readonly CodeTypeExp _exp;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write("new ");
			_exp.Compile(writer, indent, ctype, namespaces);
			CompileTemplate(writer, indent, ctype, namespaces);
			CompileParam(writer, indent, ctype, namespaces);
		}
	}

	public class CodeNewArrayExp : CodeExp
	{
		public CodeNewArrayExp(CodeTypeExp exp)
		{
			_exp = exp;
			_length = -1;
			_arrays = new List<CodeExp>();
		}

		public int length
		{
			set { _length = value; }
		}

		public List<CodeExp> arrays
		{
			get { return _arrays; }
		}

		protected readonly CodeTypeExp _exp;
		protected int _length;
		protected readonly List<CodeExp> _arrays;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write("new ");
			_exp.Compile(writer, indent, ctype, namespaces);
			if (_length >= 0)
			{
				writer.Write("[{0}]", _length);
			}
			else if (_arrays.Count == 0)
			{
				writer.Write("[0]");
			}
			else
			{
				writer.Write("[]");
			}
			if (_arrays.Count > 0)
			{
				writer.WriteLine(" {");
				string newindent = indent + "\t";
				foreach (CodeExp exp in _arrays)
				{
					writer.Write(newindent);
					exp.Compile(writer, newindent, ctype, namespaces);
					writer.WriteLine(",");
				}
				writer.Write(indent);
				writer.Write("}");
			}
		}
	}

	public abstract class CodeStat
	{
		public virtual void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces) {}

		public virtual void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num) {}
	}

	public class CodeTextStat : CodeStat
	{
		public CodeTextStat(string text)
		{
			_text = text;
		}

		protected readonly string _text;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.WriteLine(_text);
		}
	}

	public class CodeExpStat : CodeStat
	{
		public CodeExpStat(CodeExp exp)
		{
			_exp = exp;
		}

		protected readonly CodeExp _exp;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_exp.PreCompile(ctype, block, num);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			_exp.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(";");
		}
	}

	public class CodeVariableStat : CodeStat
	{
		public CodeVariableStat(CodeVariable var)
		{
			_var = var;
		}

		protected readonly CodeVariable _var;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			block.hasvalues.Add(_var, true);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			_var.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(";");
		}
	}

	public class CodeCommentStat : CodeStat
	{
		public CodeCommentStat(CodeComment comments)
		{
			_comments = comments;
		}

		protected readonly CodeComment _comments;

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			_comments.Compile(writer, indent);
		}
	}

	public class CodeThrowStat : CodeStat
	{
		public CodeThrowStat(CodeExp exp)
		{
			_exp = exp;
		}

		protected readonly CodeExp _exp;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_exp.PreCompile(ctype, block, num);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.Write("throw ");
			_exp.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(";");
		}
	}

	public class CodeReturnStat : CodeStat
	{
		public CodeReturnStat()
		{
			_exp = null;
		}

		public CodeReturnStat(CodeExp exp)
		{
			_exp = exp;
		}

		protected readonly CodeExp _exp;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			if (_exp != null)
				_exp.PreCompile(ctype, block, num);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			if (_exp != null)
			{
				writer.Write(indent);
				writer.Write("return ");
				_exp.Compile(writer, indent, ctype, namespaces);
				writer.WriteLine(";");
			}
			else
			{
				writer.Write(indent);
				writer.WriteLine("return;");
			}
		}
	}

	public class CodeBreakStat : CodeStat
	{
		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.WriteLine("break;");
		}
	}

	public class CodeContinueStat : CodeStat
	{
		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.WriteLine("continue;");
		}
	}

	public class CodeBlockStat : CodeStat
	{
		public CodeBlockStat()
		{
			_stats = new List<CodeStat>();
			_values = new Dictionary<CodeVariable, int>();
			_sortvalues = new Dictionary<int, List<CodeVariable>>();
			_upvalues = new Dictionary<CodeVariable, bool>();
			_hasvalues = new Dictionary<CodeVariable, bool>();
		}

		public bool Contain(CodeVariable var)
		{
			return _values.ContainsKey(var) || _upvalues.ContainsKey(var);
		}

		public List<CodeStat> stats
		{
			get { return _stats; }
		}

		public Dictionary<CodeVariable, int> values
		{
			get { return _values; }
		}

		public Dictionary<CodeVariable, bool> hasvalues
		{
			get { return _hasvalues; }
		}

		protected readonly List<CodeStat> _stats;
		protected readonly Dictionary<CodeVariable, int> _values;
		protected readonly Dictionary<int, List<CodeVariable>> _sortvalues;
		protected Dictionary<CodeVariable, bool> _upvalues;
		protected readonly Dictionary<CodeVariable, bool> _hasvalues;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_values.Clear();
			_upvalues.Clear();
			_hasvalues.Clear();
			if (block != null)
			{
				_upvalues = new Dictionary<CodeVariable, bool>(block._upvalues);
				foreach (KeyValuePair<CodeVariable, int> pair in block._values)
				{
					if (!_upvalues.ContainsKey(pair.Key))
						_upvalues.Add(pair.Key, true);
				}
				foreach (KeyValuePair<CodeVariable, bool> pair in block._hasvalues)
				{
					if (!_upvalues.ContainsKey(pair.Key))
						_upvalues.Add(pair.Key, true);
				}
			}
			for (int i = 0; i < _stats.Count; ++i)
			{
				_stats[i].PreCompile(ctype, this, i);
			}
			if (_values.Count > 0)
			{
				_sortvalues.Clear();
				foreach (KeyValuePair<CodeVariable, int> pair in _values)
				{
					List<CodeVariable> varlist;
					if (!_sortvalues.TryGetValue(pair.Value, out varlist))
					{
						varlist = new List<CodeVariable>();
						_sortvalues.Add(pair.Value, varlist);
					}
					varlist.Add(pair.Key);
				}
			}
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			if (_stats.Count == 1)
			{
				string newindent = indent + "\t";
				_stats[0].Compile(writer, newindent, ctype, namespaces);
			}
			else
			{
				writer.Write(indent);
				writer.WriteLine("{");
				string newindent = indent + "\t";
				for (int i = 0; i < _stats.Count; ++i)
				{
					List<CodeVariable> varlist;
					if (_sortvalues.TryGetValue(i, out varlist))
					{
						foreach (CodeVariable var in varlist)
						{
							if (!_hasvalues.ContainsKey(var))
							{
								new CodeVariableStat(var).Compile(writer, newindent, ctype, namespaces);
							}
						}
					}
					_stats[i].Compile(writer, newindent, ctype, namespaces);
				}
				writer.Write(indent);
				writer.WriteLine("}");
			}
		}
	}

	public class CodeDisposeStat : CodeStat
	{
		public CodeDisposeStat(CodeExp exp)
		{
			_exp = exp;
			_block = new CodeBlockStat();
		}

		public CodeBlockStat stat
		{
			get { return _block; }
		}

		protected readonly CodeExp _exp;
		protected readonly CodeBlockStat _block;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_exp.PreCompile(ctype, block, num);
			_block.PreCompile(ctype, block, num);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.Write("using (");
			_exp.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(")");
			_block.Compile(writer, indent, ctype, namespaces);
		}
	}

	public class CodeTryStat : CodeStat
	{
		public CodeTryStat()
		{
			_try = new CodeBlockStat();
			_finally = new CodeBlockStat();
			_catchs = new List<CodeCatchStat>();
		}

		public CodeBlockStat trystat
		{
			get { return _try; }
		}

		public CodeBlockStat finallystat
		{
			get { return _finally; }
		}

		public List<CodeCatchStat> catchstat
		{
			get { return _catchs; }
		}

		protected readonly CodeBlockStat _try;
		protected readonly CodeBlockStat _finally;
		protected readonly List<CodeCatchStat> _catchs;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_try.PreCompile(ctype, block, num);
			for (int i = 0; i < _catchs.Count; ++i)
			{
				_catchs[i].PreCompile(ctype, block, num);
			}
			_finally.PreCompile(ctype, block, num);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.WriteLine("try");
			if (_try.stats.Count == 1)
			{
				writer.Write(indent);
				writer.WriteLine("{");
				_try.Compile(writer, indent, ctype, namespaces);
				writer.Write(indent);
				writer.WriteLine("}");
			}
			else
			{
				_try.Compile(writer, indent, ctype, namespaces);
			}
			if (_catchs.Count == 0)
			{
				writer.Write(indent);
				writer.WriteLine("finally");
				if (_finally.stats.Count == 1)
				{
					writer.Write(indent);
					writer.WriteLine("{");
					_finally.Compile(writer, indent, ctype, namespaces);
					writer.Write(indent);
					writer.WriteLine("}");
				}
				else
				{
					_finally.Compile(writer, indent, ctype, namespaces);
				}
			}
			else
			{
				for (int i = 0; i < _catchs.Count; ++i)
				{
					_catchs[i].Compile(writer, indent, ctype, namespaces);
				}
				if (_finally.stats.Count != 0)
				{
					writer.Write(indent);
					writer.WriteLine("finally");
					if (_finally.stats.Count == 1)
					{
						writer.Write(indent);
						writer.WriteLine("{");
						_finally.Compile(writer, indent, ctype, namespaces);
						writer.Write(indent);
						writer.WriteLine("}");
					}
					else
					{
						_finally.Compile(writer, indent, ctype, namespaces);
					}
				}
			}
		}
	}

	public class CodeCatchStat
	{
		public CodeCatchStat()
		{
			_param = null;
			_block = new CodeBlockStat();
		}

		public CodeCatchStat(CodeTypeExp type, string name)
		{
			_param = new CodeParam(type, name);
			_block = new CodeBlockStat();
		}

		public CodeBlockStat stat
		{
			get { return _block; }
		}

		protected readonly CodeParam _param;
		protected readonly CodeBlockStat _block;

		public void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_block.PreCompile(ctype, block, num);
		}

		public void Compile(TextWriter writer, string indent, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			if (_param != null)
			{
				writer.Write("catch (");
				_param.Compile(writer, indent, ctype, namespaces);
				writer.WriteLine(")");
			}
			else
			{
				writer.WriteLine("catch");
			}
			if (_block.stats.Count == 1)
			{
				writer.Write(indent);
				writer.WriteLine("{");
				_block.Compile(writer, indent, ctype, namespaces);
				writer.Write(indent);
				writer.WriteLine("}");
			}
			else
			{
				_block.Compile(writer, indent, ctype, namespaces);
			}
		}
	}

	public class CodeIfStat : CodeStat
	{
		public CodeIfStat(CodeExp exp)
		{
			_condition = exp;
			_if = new CodeBlockStat();
			_else = new CodeBlockStat();
			_elseifs = new List<CodeElseIfStat>();
		}

		public CodeBlockStat ifstat
		{
			get { return _if; }
		}

		public CodeBlockStat elsestat
		{
			get { return _else; }
		}

		public List<CodeElseIfStat> elseifstat
		{
			get { return _elseifs; }
		}

		protected readonly CodeBlockStat _if;
		protected readonly CodeBlockStat _else;
		protected readonly List<CodeElseIfStat> _elseifs;
		protected readonly CodeExp _condition;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_if.PreCompile(ctype, block, num);
			for (int i = 0; i < _elseifs.Count; ++i)
			{
				_elseifs[i].PreCompile(ctype, block, num);
			}
			_else.PreCompile(ctype, block, num);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.Write("if (");
			_condition.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(")");
			_if.Compile(writer, indent, ctype, namespaces);
			for (int i = 0; i < _elseifs.Count; ++i)
			{
				_elseifs[i].Compile(writer, indent, ctype, namespaces);
			}
			if (_else.stats.Count != 0)
			{
				writer.Write(indent);
				writer.WriteLine("else");
				_else.Compile(writer, indent, ctype, namespaces);
			}
		}
	}

	public class CodeElseIfStat
	{
		public CodeElseIfStat(CodeExp exp)
		{
			_condition = exp;
			_block = new CodeBlockStat();
		}

		public CodeBlockStat stat
		{
			get { return _block; }
		}

		protected readonly CodeExp _condition;
		protected readonly CodeBlockStat _block;

		public void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_block.PreCompile(ctype, block, num);
		}

		public void Compile(TextWriter writer, string indent, CodeTypeDefine ctype, IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.Write("else if (");
			_condition.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(")");
			_block.Compile(writer, indent, ctype, namespaces);
		}
	}

	public class CodeForStat : CodeStat
	{
		public CodeForStat()
		{
			_block = new CodeBlockStat();
		}

		public CodeBlockStat stat
		{
			get { return _block; }
		}

		protected CodeVariable _var;
		protected CodeExp _exp;
		protected CodeExp _inc;
		protected readonly CodeBlockStat _block;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_block.PreCompile(ctype, block, num);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.Write("for (");
			if (_var != null)
				_var.Compile(writer, indent, ctype, namespaces);
			writer.Write("; ");
			if (_exp != null)
				_exp.Compile(writer, indent, ctype, namespaces);
			writer.Write("; ");
			if (_inc != null)
				_inc.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(")");
			_block.Compile(writer, indent, ctype, namespaces);
		}
	}

	public class CodeForEachStat : CodeStat
	{
		public CodeForEachStat()
		{
			_block = new CodeBlockStat();
		}

		public CodeBlockStat stat
		{
			get { return _block; }
		}

		protected CodeVariable _var;
		protected CodeExp _iterate;
		protected readonly CodeBlockStat _block;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_block.PreCompile(ctype, block, num);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.Write("foreach (");
			_var.Compile(writer, indent, ctype, namespaces);
			writer.Write(" in ");
			_iterate.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(")");
			_block.Compile(writer, indent, ctype, namespaces);
		}
	}

	public class CodeWhileStat : CodeBlockStat
	{
		public CodeWhileStat(CodeExp exp)
		{
			_condition = exp;
			_block = new CodeBlockStat();
		}

		public CodeBlockStat stat
		{
			get { return _block; }
		}

		protected readonly CodeExp _condition;
		protected readonly CodeBlockStat _block;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_condition.PreCompile(ctype, block, num);
			_block.PreCompile(ctype, block, num);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.Write("while (");
			_condition.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(")");
			_block.Compile(writer, indent, ctype, namespaces);
		}
	}

	public class CodeDoWhileStat : CodeBlockStat
	{
		public CodeDoWhileStat(CodeExp exp)
		{
			_condition = exp;
			_block = new CodeBlockStat();
		}

		public CodeBlockStat stat
		{
			get { return _block; }
		}

		protected readonly CodeExp _condition;
		protected readonly CodeBlockStat _block;

		public override void PreCompile(CodeTypeDefine ctype, CodeBlockStat block, int num)
		{
			_condition.PreCompile(ctype, block, num);
			_block.PreCompile(ctype, block, num);
		}

		public override void Compile(TextWriter writer, string indent, CodeTypeDefine ctype,
									IDictionary<string, bool> namespaces)
		{
			writer.Write(indent);
			writer.WriteLine("do");
			_block.Compile(writer, indent, ctype, namespaces);
			writer.Write(indent);
			writer.Write("while (");
			_condition.Compile(writer, indent, ctype, namespaces);
			writer.WriteLine(");");
		}
	}
}