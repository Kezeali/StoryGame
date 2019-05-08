using System.Text;

// String formatting utils that don't allocate a new StringBuilder every time you call them.
public static class Strf
{
	public static StringBuilder stringBuilder = new StringBuilder(1000);

	public static string Format(string format, object p1)
	{
		stringBuilder.Length = 0;
		stringBuilder.AppendFormat(format, p1);
		return stringBuilder.ToString();
	}

	public static string Format(string format, object p1, object p2)
	{
		stringBuilder.Length = 0;
		stringBuilder.AppendFormat(format, p1, p2);
		return stringBuilder.ToString();
	}

	public static string Format(string format, object p1, object p2, object p3)
	{
		stringBuilder.Length = 0;
		stringBuilder.AppendFormat(format, p1, p2, p3);
		return stringBuilder.ToString();
	}

	public static string Format(string format, object p1, object p2, object p3, object p4)
	{
		stringBuilder.Length = 0;
		stringBuilder.AppendFormat(format, p1, p2, p3, p4);
		return stringBuilder.ToString();
	}
}
