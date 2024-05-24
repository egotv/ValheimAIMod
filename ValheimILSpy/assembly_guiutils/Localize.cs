using System;
using UnityEngine;

public class Localize : MonoBehaviour
{
	private void Start()
	{
		Localization.instance.Localize(base.transform);
		Localization.OnLanguageChange = (Action)Delegate.Combine(Localization.OnLanguageChange, new Action(RelocalizeAllUponChange));
		ZInput.OnInputLayoutChanged += RelocalizeAllUponChange;
	}

	private void OnDestroy()
	{
		ZInput.OnInputLayoutChanged -= RelocalizeAllUponChange;
		Localization.OnLanguageChange = (Action)Delegate.Remove(Localization.OnLanguageChange, new Action(RelocalizeAllUponChange));
	}

	private void RelocalizeAllUponChange()
	{
		Localization.instance.ReLocalizeAll(base.transform);
	}

	public void RefreshLocalization()
	{
		Localization.instance.ReLocalizeVisible(base.transform);
	}
}
