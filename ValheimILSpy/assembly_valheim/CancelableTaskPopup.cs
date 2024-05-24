using System.Collections;

public class CancelableTaskPopup : LivePopupBase
{
	public readonly PopupButtonCallback cancelCallback;

	public override PopupType Type => PopupType.CancelableTask;

	public CancelableTaskPopup(RetrieveFromStringSource headerRetrievalFunc, RetrieveFromStringSource textRetrievalFunc, RetrieveFromBoolSource shouldCloseRetrievalFunc, PopupButtonCallback cancelCallback)
		: base(headerRetrievalFunc, textRetrievalFunc, shouldCloseRetrievalFunc)
	{
		SetUpdateRoutine(UpdateRoutine());
		this.cancelCallback = cancelCallback;
	}

	private IEnumerator UpdateRoutine()
	{
		while (!shouldCloseRetrievalFunc())
		{
			headerText.text = headerRetrievalFunc();
			bodyText.text = textRetrievalFunc();
			yield return null;
		}
		base.ShouldClose = true;
	}
}
