using System;
using System.IO;

public class FileWriter
{
	public enum WriterStatus
	{
		OpenSucceeded,
		OpenFailed,
		CloseSucceeded,
		CloseFailed
	}

	public BinaryWriter m_binary;

	public StreamWriter m_stream;

	private string m_path;

	private MemoryStream m_mem;

	private FileStream m_file;

	public FileHelpers.FileHelperType m_type { get; private set; }

	public FileHelpers.FileSource m_fileSource { get; private set; }

	public WriterStatus Status { get; private set; }

	public FileWriter(string path, FileHelpers.FileHelperType type = FileHelpers.FileHelperType.Binary, FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		m_path = path;
		m_type = type;
		m_fileSource = ((!FileHelpers.m_cloudEnabled || (fileSource != 0 && fileSource != FileHelpers.FileSource.Cloud)) ? FileHelpers.FileSource.Local : FileHelpers.FileSource.Cloud);
		if (fileSource == FileHelpers.FileSource.Legacy)
		{
			Status = WriterStatus.OpenFailed;
			return;
		}
		if (m_fileSource == FileHelpers.FileSource.Cloud)
		{
			m_mem = new MemoryStream();
			if (m_type == FileHelpers.FileHelperType.Binary)
			{
				m_binary = new BinaryWriter(m_mem);
			}
			else
			{
				if (m_type != FileHelpers.FileHelperType.Stream)
				{
					Status = WriterStatus.OpenFailed;
					throw new NotImplementedException();
				}
				m_stream = new StreamWriter(m_mem);
			}
		}
		else
		{
			FileHelpers.EnsureDirectoryExists(m_path);
			m_file = File.Create(m_path);
			if (m_type == FileHelpers.FileHelperType.Binary)
			{
				m_binary = new BinaryWriter(m_file);
			}
			else
			{
				if (m_type != FileHelpers.FileHelperType.Stream)
				{
					Status = WriterStatus.OpenFailed;
					throw new NotImplementedException();
				}
				m_stream = new StreamWriter(m_file);
			}
		}
		Status = WriterStatus.OpenSucceeded;
	}

	public void Finish()
	{
		if (Status == WriterStatus.OpenFailed)
		{
			Status = WriterStatus.CloseFailed;
			return;
		}
		WriterStatus status = WriterStatus.CloseSucceeded;
		m_binary?.Flush();
		m_stream?.Flush();
		if (m_fileSource == FileHelpers.FileSource.Cloud)
		{
			byte[] array = m_mem.ToArray();
			ZLog.Log($"Cloud Save: {array.Length} bytes. {m_path}");
			if (!FileHelpers.CloudFileWriteInChunks(m_path, array))
			{
				status = WriterStatus.CloseFailed;
			}
		}
		if (m_file != null)
		{
			m_file.Flush(flushToDisk: true);
		}
		m_binary?.Close();
		m_stream?.Close();
		Status = status;
	}

	public void DumpCloudWriteToLocalFile(string localPath)
	{
		FileHelpers.EnsureDirectoryExists(localPath);
		FileStream fileStream = File.Create(localPath);
		switch (m_type)
		{
		case FileHelpers.FileHelperType.Binary:
		{
			BinaryWriter binaryWriter = new BinaryWriter(fileStream);
			binaryWriter.Write(m_mem.ToArray());
			binaryWriter.Flush();
			binaryWriter.Close();
			break;
		}
		case FileHelpers.FileHelperType.Stream:
		{
			StreamWriter streamWriter = new StreamWriter(fileStream);
			streamWriter.Write(m_mem.ToArray());
			streamWriter.Flush();
			streamWriter.Close();
			break;
		}
		default:
			throw new NotImplementedException();
		}
	}

	public static explicit operator BinaryWriter(FileWriter w)
	{
		return w.m_binary;
	}

	public static explicit operator StreamWriter(FileWriter w)
	{
		return w.m_stream;
	}
}
