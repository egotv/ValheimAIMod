public interface ISerializableParameter
{
	void Serialize(ref ZPackage pkg);

	void Deserialize(ref ZPackage pkg);
}
