using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sunny.NetCore.Extension.Converter;
using Sunny.NetCore.Extension.Generator;
using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Sunny.NetCore.Extension.Test
{
	[TestClass]
	public class UnitTest1
	{
		System.Text.Json.JsonSerializerOptions jsonOptions = new System.Text.Json.JsonSerializerOptions
		{
			Converters = { GuidInterface.Singleton }
		};
		[TestMethod]
		public void TestGuid()
		{
			var guid = Guid.NewGuid();
			var str = GuidInterface.Singleton.GuidToString(ref guid);
			Assert.IsTrue(GuidInterface.Singleton.TryParse(str, out var ng));
			Assert.AreEqual(guid, ng);
			str = System.Text.Json.JsonSerializer.Serialize(guid, jsonOptions);
			ng = System.Text.Json.JsonSerializer.Deserialize<Guid>(str, jsonOptions);
			Assert.AreEqual(guid, ng);
		}
		[TestMethod]
		public void TestLong()
		{
			var l = LongValueGenerator.NextValue();
			var str = LongInterface.Singleton.LongToString(l);
			Assert.IsTrue(LongInterface.Singleton.TryParse(str, out var nl));
			Assert.AreEqual(l, nl);
		}
		[TestMethod]
		public void TestInt()
		{
			var i = Guid.NewGuid().GetHashCode();
			var str = IntInterface.Singleton.IntToString(i);
			Assert.IsTrue(IntInterface.Singleton.TryParseInt(str, out var ni));
			Assert.AreEqual(i, ni);
		}
		[TestMethod]
		public void TestDateTime()
		{
			var dt = DateTime.Today;
			var str = DateFormat.Singleton.DateTimeToString(dt);
			Assert.IsTrue(DateFormat.Singleton.TryParseDateTime(str, out var ndt));
			Assert.AreEqual(dt, ndt);

			dt = DateTime.UtcNow;
			dt = new DateTime(dt.Ticks - (dt.Ticks % TimeSpan.TicksPerSecond));
			str = DateFormat.Singleton.DateTimeToString(dt);
			Assert.IsTrue(DateFormat.Singleton.TryParseDateTime(str, out ndt));
			Assert.AreEqual(dt, ndt);

			Assert.IsTrue(DateFormat.Singleton.TryParseDateTime("2020-8-8", out dt));
			Assert.AreEqual(dt, new DateTime(2020, 8, 8));
		}
	}
}
