
using System.Runtime.CompilerServices;
using System.Xml;
using System;
using CoreFramework;

namespace CoreFramework
{
	public static class XmlHelper
	{
		public static IDisposable WriteElement(this XmlWriter writer, string localName, string ns) 
		{
			return new AutoCleanup(()=>writer.WriteStartElement(localName, ns),
								   ()=>writer.WriteEndElement());
		}
		public static IDisposable WriteElement(this XmlWriter writer, string localName)
		{
			return new AutoCleanup(()=>writer.WriteStartElement(localName),
								   ()=>writer.WriteEndElement());
		}

		public static void WriteElementValue(this XmlWriter writer, string localName, int value)
		{
			using(writer.WriteElement(localName))
			{
				writer.WriteValue(value);
			}
		}
		public static void WriteElementValue(this XmlWriter writer, string localName, DateTime value)
		{
			using(writer.WriteElement(localName))
			{
				writer.WriteValue(value);
			}
		}
		public static void WriteElementValue(this XmlWriter writer, string localName, string value)
		{
			using(writer.WriteElement(localName))
			{
				writer.WriteValue(value);
			}
		}
		public static void WriteElementValue(this XmlWriter writer, string localName, DateTimeOffset value)
		{
			using(writer.WriteElement(localName))
			{
				writer.WriteValue(value);
			}
		}
		public static void WriteElementValue(this XmlWriter writer, string localName, decimal value)
		{
			using(writer.WriteElement(localName))
			{
				writer.WriteValue(value);
			}
		}
		public static void WriteElementValue(this XmlWriter writer, string localName, double value)
		{
			using(writer.WriteElement(localName))
			{
				writer.WriteValue(value);
			}
		}
		public static void WriteElementValue(this XmlWriter writer, string localName, long value)
		{
			using(writer.WriteElement(localName))
			{
				writer.WriteValue(value);
			}
		}
		public static void WriteElementValue(this XmlWriter writer, string localName, float value)
		{
			using(writer.WriteElement(localName))
			{
				writer.WriteValue(value);
			}
		}
		public static void WriteElementValue(this XmlWriter writer, string localName, object value)
		{
			using(writer.WriteElement(localName))
			{
				writer.WriteValue(value);
			}
		}
		public static void WriteElementValue(this XmlWriter writer, string localName, bool value)
		{
			using(writer.WriteElement(localName))
			{
				writer.WriteValue(value);
			}
		}
	
	}
}