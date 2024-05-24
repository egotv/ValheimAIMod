public class WarningPopup : FixedPopupBase
{
	public readonly PopupButtonCallback okCallback;

	public override PopupType Type => PopupType.Warning;

	public WarningPopup(string header, string text, PopupButtonCallback okCallback, bool localizeText = true)
		: base(header, text, localizeText)
	{
		this.okCallback = okCallback;
	}
}
