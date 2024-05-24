using System.Collections;
using SoftReferenceableAssets.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
	public SceneReference m_scene;

	private bool _showLogos = true;

	private bool _showHealthWarning;

	private bool _showSaveNotification;

	private ILoadSceneAsyncOperation _sceneLoadOperation;

	private ThreadPriority _currentLoadingBudgetRequest;

	private float _fakeProgress;

	[SerializeField]
	private GameObject gameLogo;

	[SerializeField]
	private GameObject coffeeStainLogo;

	[SerializeField]
	private GameObject ironGateLogo;

	[SerializeField]
	private CanvasGroup savingNotification;

	[SerializeField]
	private CanvasGroup healthWarning;

	public AnimationCurve alphaCurve;

	public AnimationCurve scalingCurve;

	private const float LogoDisplayTime = 2f;

	private const float SaveNotificationDisplayTime = 5f;

	private const float HealthWarningDisplayTime = 5f;

	private const float FadeInOutTime = 0.5f;

	private void Awake()
	{
		_showLogos = true;
		_showHealthWarning = false;
		_showSaveNotification = false;
		healthWarning.gameObject.SetActive(value: false);
		savingNotification.gameObject.SetActive(value: false);
		coffeeStainLogo.SetActive(value: false);
		ironGateLogo.SetActive(value: false);
		gameLogo.SetActive(value: false);
	}

	private void Start()
	{
		StartLoading();
	}

	private void Update()
	{
		if (!LoadingIndicator.s_instance.m_showProgressIndicator)
		{
			return;
		}
		float num = ((_sceneLoadOperation == null) ? 0f : _sceneLoadOperation.Progress);
		if (num <= 0.25f)
		{
			float num2 = num / 0.25f * 0.05f;
			if (_fakeProgress < num2)
			{
				_fakeProgress = num2;
			}
			else if (num == 0.25f)
			{
				_fakeProgress = Mathf.Min(num, _fakeProgress + Time.deltaTime * 0.01f);
			}
		}
		else
		{
			_fakeProgress = num;
		}
		LoadingIndicator.SetProgress(_fakeProgress);
	}

	private void OnDestroy()
	{
		if (_currentLoadingBudgetRequest != 0)
		{
			BackgroundLoadingBudgetController.ReleaseLoadingBudgetRequest(_currentLoadingBudgetRequest);
		}
	}

	private void StartLoading()
	{
		LoginHelper.OnLoginDone -= StartLoading;
		StartCoroutine(LoadSceneAsync());
	}

	private IEnumerator LoadSceneAsync()
	{
		SceneReference scene = m_scene;
		ZLog.Log("Starting to load scene:" + scene.ToString());
		_sceneLoadOperation = SceneManager.LoadSceneAsync(m_scene);
		_currentLoadingBudgetRequest = BackgroundLoadingBudgetController.RequestLoadingBudget(ThreadPriority.Normal);
		_sceneLoadOperation.AllowSceneActivation = false;
		if (_showLogos)
		{
			Image componentInChildren = coffeeStainLogo.GetComponentInChildren<Image>();
			Image igImage = ironGateLogo.GetComponentInChildren<Image>();
			yield return FadeLogo(coffeeStainLogo, componentInChildren, 2f, alphaCurve, scalingCurve);
			coffeeStainLogo.SetActive(value: false);
			yield return FadeLogo(ironGateLogo, igImage, 2f, alphaCurve, scalingCurve);
			ironGateLogo.SetActive(value: false);
		}
		if (_showSaveNotification)
		{
			yield return ShowSaveNotification();
		}
		if (_showHealthWarning)
		{
			yield return ShowHealthWarning();
		}
		gameLogo.SetActive(value: true);
		_currentLoadingBudgetRequest = BackgroundLoadingBudgetController.UpdateLoadingBudgetRequest(_currentLoadingBudgetRequest, ThreadPriority.High);
		LoadingIndicator.SetVisibility(visible: true);
		while (!_sceneLoadOperation.IsLoadedButNotActivated)
		{
			yield return null;
		}
		LoadingIndicator.SetVisibility(visible: false);
		while (!LoadingIndicator.IsCompletelyInvisible)
		{
			yield return null;
		}
		yield return null;
		_sceneLoadOperation.AllowSceneActivation = true;
	}

	private IEnumerator ShowSaveNotification()
	{
		savingNotification.alpha = 0f;
		savingNotification.gameObject.SetActive(value: true);
		yield return null;
		LayoutRebuilder.ForceRebuildLayoutImmediate(healthWarning.transform as RectTransform);
		float fadeTimer2 = 0f;
		while (savingNotification.alpha < 1f)
		{
			float t = 1f - (0.5f - fadeTimer2) / 0.5f;
			float alpha = Mathf.SmoothStep(0f, 1f, t);
			savingNotification.alpha = alpha;
			fadeTimer2 += Time.unscaledDeltaTime;
			yield return null;
		}
		yield return new WaitForSeconds(5f);
		fadeTimer2 = 0f;
		while (savingNotification.alpha > 0f)
		{
			float t = 1f - (0.5f - fadeTimer2) / 0.5f;
			float alpha = Mathf.SmoothStep(savingNotification.alpha, 0f, t);
			savingNotification.alpha = alpha;
			fadeTimer2 += Time.unscaledDeltaTime;
			yield return null;
		}
		savingNotification.gameObject.SetActive(value: false);
	}

	private IEnumerator ShowHealthWarning()
	{
		healthWarning.alpha = 0f;
		healthWarning.gameObject.SetActive(value: true);
		yield return null;
		LayoutRebuilder.ForceRebuildLayoutImmediate(healthWarning.transform as RectTransform);
		float fadeTimer2 = 0f;
		while (healthWarning.alpha < 1f)
		{
			float t = 1f - (0.5f - fadeTimer2) / 0.5f;
			float alpha = Mathf.SmoothStep(0f, 1f, t);
			healthWarning.alpha = alpha;
			fadeTimer2 += Time.unscaledDeltaTime;
			yield return null;
		}
		yield return new WaitForSeconds(5f);
		fadeTimer2 = 0f;
		while (healthWarning.alpha > 0f)
		{
			float t = 1f - (0.5f - fadeTimer2) / 0.5f;
			float alpha = Mathf.SmoothStep(healthWarning.alpha, 0f, t);
			healthWarning.alpha = alpha;
			fadeTimer2 += Time.unscaledDeltaTime;
			yield return null;
		}
		healthWarning.gameObject.SetActive(value: false);
	}

	private IEnumerator FadeLogo(GameObject parentGameObject, Image logo, float duration, AnimationCurve alpha, AnimationCurve scale)
	{
		Color spriteColor = logo.color;
		float timer = 0f;
		parentGameObject.SetActive(value: true);
		while (timer < duration)
		{
			float a = alpha.Evaluate(timer);
			spriteColor.a = a;
			logo.color = spriteColor;
			a = scale.Evaluate(timer);
			logo.transform.localScale = Vector3.one * a;
			timer += Time.deltaTime;
			yield return null;
		}
	}
}
