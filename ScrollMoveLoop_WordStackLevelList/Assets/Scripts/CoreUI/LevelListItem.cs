using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

	public class LevelListItem : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private Button		itemButton		= null;
		[SerializeField] private Text		levelNumberText	= null; 
		[SerializeField] private Text		levelHintText	= null;
		[SerializeField] private GameObject playIcon		= null;
		[SerializeField] private GameObject lockedIcon		= null;

		#endregion

		#region Member Variables

		private LevelData levelData;

		#endregion

		#region Properties

		public System.Action<LevelData> OnLevelItemSelected { get; set; }

		#endregion

		#region Public Methods

		public void Setup(LevelData levelData)
		{
			this.levelData = levelData;

			levelNumberText.text	= string.Format("Level {0}", levelData.Lv);
			levelHintText.text		= levelData.Hint;

			bool isLocked = GameManager.Instance.IsLevelLocked(levelData.Lv);

			itemButton.interactable = !isLocked;

			playIcon.SetActive(!isLocked);
			levelHintText.gameObject.SetActive(!isLocked);

			lockedIcon.SetActive(isLocked);
		}

		public void OnClicked()
		{
			if (OnLevelItemSelected != null)
			{
				OnLevelItemSelected(levelData);
			}
		}

		#endregion
	}
