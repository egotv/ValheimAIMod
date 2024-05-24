public class YesNoPopup : FixedPopupBase
{
	public readonly PopupButtonCallback yesCallback;

	public readonly PopupButtonCallback noCallback;

	public override PopupType Type => PopupType.YesNo;

	public YesNoPopup(string header, string text, PopupButtonCallback yesCallback, PopupButtonCallback noCallback, bool localizeText = true)
		: base(header, text, localizeText)
	{
		this.yesCallback = yesCallback;
		this.noCallback = noCallback;
	}
}
