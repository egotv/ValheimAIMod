using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Valheim/SeasonalItemGroup")]
public class SeasonalItemGroup : ScriptableObject
{
	[Header("Dates (Day, Month)")]
	[SerializeField]
	[Tooltip("(Day, Month), date at which the event starts. Month will be limited to 1 - 12 and days to 1 - end of month.")]
	private Vector2 _startDate;

	[SerializeField]
	[Tooltip("(Day, Month), date at which the event ends. Month will be limited to 1 - 12 and days to 1 - end of month.")]
	private Vector2 _endDate;

	[Space(10f)]
	public List<GameObject> Pieces = new List<GameObject>();

	public List<Recipe> Recipes = new List<Recipe>();

	public bool IsInSeason()
	{
		if (DateTime.Now.Date >= GetStartDate().Date)
		{
			return DateTime.Now.Date <= GetEndDate().Date;
		}
		return false;
	}

	public DateTime GetStartDate()
	{
		_startDate = ConstrainDates(_startDate);
		_endDate = ConstrainDates(_endDate);
		return new DateTime((_startDate.y > _endDate.y && (float)DateTime.Now.Month <= _endDate.y) ? (DateTime.Now.Year - 1) : DateTime.Now.Year, Mathf.RoundToInt(_startDate.y), Mathf.RoundToInt(_startDate.x));
	}

	public DateTime GetEndDate()
	{
		_startDate = ConstrainDates(_startDate);
		_endDate = ConstrainDates(_endDate);
		return new DateTime((_startDate.y > _endDate.y && (float)DateTime.Now.Month > _endDate.y) ? (DateTime.Now.Year + 1) : DateTime.Now.Year, Mathf.RoundToInt(_endDate.y), Mathf.RoundToInt(_endDate.x));
	}

	private Vector2 ConstrainDates(Vector2 inVector)
	{
		inVector.y = Mathf.Clamp(inVector.y, 1f, 12f);
		if (inVector.x < 1f)
		{
			inVector.x = 1f;
		}
		else if (inVector.y == 2f && inVector.x > 28f)
		{
			inVector.x = (DateTime.IsLeapYear(DateTime.Now.Year) ? 29 : 28);
		}
		else if (inVector.x > 30f)
		{
			inVector.x = ((inVector.y % 2f == 0f) ? 30 : 31);
		}
		return inVector;
	}
}
