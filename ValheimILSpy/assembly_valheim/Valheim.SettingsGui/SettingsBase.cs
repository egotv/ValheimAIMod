using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public abstract class SettingsBase : MonoBehaviour
{
	protected enum NavigationDirection
	{
		OnLeft,
		OnRight,
		OnUp,
		OnDown
	}

	public Action Saved;

	public void OnOk()
	{
		Settings.instance.OnOk();
	}

	public virtual void OnBack()
	{
		Settings.instance.OnBack();
	}

	public abstract void FixBackButtonNavigation(Button backButton);

	public abstract void FixOkButtonNavigation(Button okButton);

	public abstract void LoadSettings();

	public abstract void SaveSettings();

	public virtual void ResetSettings()
	{
	}

	protected void SetNavigationToFirstActive(Selectable selectable, NavigationDirection direction, List<Selectable> targets)
	{
		Selectable selectable2 = targets.FirstOrDefault((Selectable t) => t.gameObject.activeSelf);
		if (!(selectable2 == null))
		{
			SetNavigation(selectable, direction, selectable2);
		}
	}

	protected void SetNavigation(Selectable selectable, NavigationDirection direction, Selectable target)
	{
		Navigation navigation = selectable.navigation;
		switch (direction)
		{
		case NavigationDirection.OnLeft:
			navigation.selectOnLeft = target;
			break;
		case NavigationDirection.OnRight:
			navigation.selectOnRight = target;
			break;
		case NavigationDirection.OnUp:
			navigation.selectOnUp = target;
			break;
		case NavigationDirection.OnDown:
			navigation.selectOnDown = target;
			break;
		}
		selectable.navigation = navigation;
	}
}
