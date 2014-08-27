public interface IObservableValue
{
	object Value { get; }
	bool HasChanged { get; }
}
