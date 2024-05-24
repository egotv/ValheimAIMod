public struct FilePathAndSource
{
	public string path;

	public FileHelpers.FileSource source;

	public FilePathAndSource(string path, FileHelpers.FileSource source)
	{
		this.path = path;
		this.source = source;
	}
}
