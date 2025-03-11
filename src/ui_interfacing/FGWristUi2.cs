using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using FistVR;
using System.Security.Cryptography;
using System;
using System.Text;
using System.Linq;

namespace NGA {
public class FGWristUi2 : MonoBehaviour {

	// Active UI state.
	private int selectedTabIx = 0; // 0: Npc; 1: Respawn; 2: Delete;
	private int currQuestTabType = 0; // 0: available, 1: active
	private int selectedQuestIndex = -1;

	[Header("UI References")]
	private Transform Canvas;
	private Transform ButtonsSection;
	private Transform TimeText;
	private Transform TravelTab; // Index 0
	private Transform ContractsTab; // Ix 1
	private Transform ContractsVertList;
	private Transform ContractStickerTempl;
	private Transform TimeTab; // Ix 2
	private Transform BankTab; // Ix 3
	private Transform RepTab; // Ix 4

	public void Start() {
		SetHandsAndFace(GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>(), 
                GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>(), 
				GM.CurrentPlayerBody.EyeCam.transform);
		FindUiVariables();
		AddTimeUiRunner();
		CloseTabs();
	}
	

	public void Update () {
		UpdateWristMenu();
		// TODO: Update clock.
	}

	// -- Paints and reacts the UI -- \\
	private void FindUiVariables() {
		Canvas = transform.Find("Canvas");
		ButtonsSection = Canvas.Find("SubmenuButs");
		TravelTab = Canvas.Find("TravelTab");
		ContractsTab = Canvas.Find("ContractsTab");
		TimeTab = Canvas.Find("TimeTab");
		BankTab = Canvas.Find("BankTab");
		RepTab = Canvas.Find("RepTab");

		// Bits.
		TimeText = Canvas.Find("Time");

		// Contracts vertical list.
		ContractsVertList = ContractsTab.Find("ContractsList");
		ContractStickerTempl = ContractsVertList.Find("ContractStickerTmpl");
	}

    public void CloseTabs()
    {
        TravelTab.gameObject.SetActive(false);
		ContractsTab.gameObject.SetActive(false);
		TimeTab.gameObject.SetActive(false);
		BankTab.gameObject.SetActive(false);
		RepTab.gameObject.SetActive(false);
    }
	public void ClearContractTexts() {
		// TODO: Do for all in vertical list
		Text descriptionText = ContractStickerTempl.Find("Description").GetComponentInChildren<Text>();
		descriptionText.text = "";
		Text longtext = ContractsTab.Find("ContractDescription").Find("Description").GetComponentInChildren<Text>();
		longtext.text = "";
	}

	// TODO: Extend so it's vertical list.
	public void BTN_DisplayQuests(int i) {
		currQuestTabType = i;
		selectedQuestIndex = -1; // Unselect previous.
		ClearContractTexts();
		List<FGContract> contracts = GetCurrentContractList();
		string tabname;
		switch (i) {
			case 0:
				tabname = "[Available]";
				break;
			case 1:
				tabname = "[Active]";
				break;
			case 2:
				tabname = "[Finished]";
				break;
			default:
				tabname = "";
				break;
		}
		Text descriptionText = ContractStickerTempl.Find("Description").GetComponentInChildren<Text>();
		foreach (FGContract fGContract in contracts) {	
			descriptionText.text = "" + fGContract.DisplayName + "\n" 
									+ fGContract.TargetFirstName + " " + fGContract.TargetLastName + "\n"
									+ fGContract.Infraction + "\n"
									+ tabname;
			break; // TODO: Do all verticals list
		}
		selectedQuestIndex = contracts.Count == 0 ? -1 : 0; // Default to select first
		BTN_DisplayQuestDetail();		
	}
	public void BTN_DisplayQuestDetail() {
		if (selectedQuestIndex < 0) {
			return;
		}
		List<FGContract> contracts = GetCurrentContractList();
		if (selectedQuestIndex >= contracts.Count) {
			return;
		}

		// TODO get quest from id
		FGContract fGContract = contracts[selectedQuestIndex];
		Text longtext = ContractsTab.Find("ContractDescription").Find("Description").GetComponentInChildren<Text>();
		longtext.text = fGContract.PrintContract();
	}
	private List<FGContract> GetCurrentContractList() {
		List<FGContract> contracts;
		switch (currQuestTabType) {
			case 0:
				contracts = FG_GM.Instance.contractMan.GetAvailableContracts();
				break;
			case 1:
				contracts = FG_GM.Instance.contractMan.GetActiveContracts();
				break;
			case 2:
				contracts = FG_GM.Instance.contractMan.GetCompletedContracts();
				break;
			default:
				contracts = new List<FGContract>();
				break;
		}
		return contracts;
	}
	public void BTN_AcceptContract() {
		if (selectedQuestIndex < 0 || currQuestTabType != 0) {
			Debug.LogWarning("Selected index is invalid " + selectedQuestIndex
							+ " or you didn't select Available tab " + currQuestTabType);
			return;
		}
		List<FGContract> contracts = GetCurrentContractList();
		if (selectedQuestIndex >= contracts.Count) {
			Debug.LogWarning("Selected index is invalid too big " + selectedQuestIndex
							+ " compared to available contracts list " + contracts.Count);
			return;
		}
		FG_GM.Instance.contractMan.AcceptContract(contracts[selectedQuestIndex].uniqueID);
		BTN_DisplayQuests(currQuestTabType);
		Debug.LogWarning("Allegedly accepted quest.");
	}
	public void BTN_RejectContract() {
		if (selectedQuestIndex < 0 || currQuestTabType != 1) {
			Debug.LogWarning("Selected index is invalid " + selectedQuestIndex
							+ " or you didn't select Active tab " + currQuestTabType);
			return;
		}
		List<FGContract> contracts = GetCurrentContractList();
		if (selectedQuestIndex >= contracts.Count) {
			Debug.LogWarning("Selected index is invalid too big " + selectedQuestIndex
							+ " compared to active contracts list " + contracts.Count);
			return;
		}
		FG_GM.Instance.contractMan.RejectContract(contracts[selectedQuestIndex].uniqueID);
		BTN_DisplayQuests(currQuestTabType);
		Debug.LogWarning("Allegedly rejected quest.");
	}
	public void BTN_TravelToContractLevel() {
		if (selectedQuestIndex < 0 || currQuestTabType != 1) {
			Debug.LogWarning("Selected index is invalid " + selectedQuestIndex
							+ " or you didn't select Active tab " + currQuestTabType);
			return;
		}
		List<FGContract> contracts = GetCurrentContractList();
		if (selectedQuestIndex >= contracts.Count) {
			Debug.LogWarning("Selected index is invalid too big " + selectedQuestIndex
							+ " compared to active contracts list " + contracts.Count);
			return;
		}
		FG_GM.Instance.TransitionToLevelFromContract(contracts[selectedQuestIndex]);
	}
	public void BTN_RefreshBankInfo() {
		BankTab.Find("Description")
			.GetComponentInChildren<Text>().text 
			= FG_GM.Instance.bank.PrintPlyBankInfo();
	}
	public void BTN_RefreshRepInfo() {
		RepTab.Find("Description")
			.GetComponentInChildren<Text>().text 
			= FG_GM.Instance.factionStance.PrintFactionStance();
	}

	public void BTN_OpenTab(int index) {
		CloseTabs();
		switch(index) {
			case 0:
				TravelTab.gameObject.SetActive(true);
				break;
			case 1:
				ContractsTab.gameObject.SetActive(true);
				BTN_DisplayQuests(currQuestTabType);
				break;
			case 2:
				TimeTab.gameObject.SetActive(true);
				break;
			case 3:
				BankTab.gameObject.SetActive(true);
				BTN_RefreshBankInfo();
				break;
			case 4:
				RepTab.gameObject.SetActive(true);
				BTN_RefreshRepInfo();
				break;
			default:
				Debug.LogError("Selected tab not supported: " + index);
				return;
		}
		selectedTabIx = index;
	}
	public void BTN_TravelHome() {
		FG_GM.Instance.TransitionToLevel("IndoorRange_Updated");
	}
    public void BTN_TravelArea() {
		FG_GM.Instance.TransitionToLevel("Grillhouse_2Story");
	}
	public void BTN_AdvanceHour() {
		FGTimeSystem.Instance.AdvanceTime(/*seconds*/3600);
	}

	public void AddTimeUiRunner() {
		FGTimeUi2 timeUI = gameObject.AddComponent<FGTimeUi2>();

		Text timeText = TimeText.GetComponentInChildren<Text>();
		if (timeText != null)
		{
			timeUI.timeText = timeText;
		}
	}


	// -- Wrist functionality -- \\
	public Transform Face;
	public FVRViveHand[] Hands = new FVRViveHand[2];
	private FVRViveHand m_currentHand;
	private bool m_hasHands = false;
	private bool m_isActive = false;
	private float m_wristPointRange = 60f;
	private float m_faceAngleRange = 45f;

	private void UpdateWristMenu()
	{
		if (!this.m_hasHands)
		{
			return;
		}
		if (this.m_isActive)
		{
			PositionWristMenu();
			if (this.m_currentHand.CurrentInteractable != null 
					|| Vector3.Angle(this.m_currentHand.GetWristMenuTarget().forward, -this.Face.forward) 
						>= this.m_wristPointRange)
			{
				this.Deactivate();
				return;
			}
			if (this.m_currentHand != null)
			{
				Vector3 vector = this.m_currentHand.GetWristMenuTarget().position - this.Face.position;
				if (Vector3.Angle(this.Face.forward, vector) >= this.m_faceAngleRange)
				{
					this.Deactivate();
					return;
				}
			}
		}
		if (!this.m_isActive)
		{
			for (int i = 0; i < this.Hands.Length; i++)
			{
				if (this.Hands[i].CurrentInteractable == null
					&& Vector3.Angle(this.Hands[i].GetWristMenuTarget().forward, -this.Face.forward) 
						< this.m_wristPointRange)
				{
					Vector3 vector2 = this.Hands[i].GetWristMenuTarget().position - this.Face.position;
					if (!this.Hands[i].IsThisTheRightHand || GM.Options.ControlOptions.WristMenuState != ControlOptions.WristMenuMode.LeftHand)
					{
						if (this.Hands[i].IsThisTheRightHand || GM.Options.ControlOptions.WristMenuState != ControlOptions.WristMenuMode.RightHand)
						{
							if (Vector3.Angle(this.Face.forward, vector2) < this.m_faceAngleRange)
							{
								this.ActivateOnHand(this.Hands[i]);
							}
						}
					}
				}
			}
		}
	}

	public void SetHandsAndFace(FVRViveHand Hand0, FVRViveHand Hand1, Transform face)
	{
		Hands = new FVRViveHand[2];
		Hands[0] = Hand0;
		Hands[1] = Hand1;
		Face = face;
		m_hasHands = true;
	}

	public void PositionWristMenu()
	{
		if (this.m_isActive && this.m_currentHand != null)
		{
			Transform target = this.m_currentHand.GetWristMenuTarget();

			// Move the position 10 cm (0.1m) to the right (assuming local right direction)
			transform.position = target.position + target.right * -0.17f;

			// Rotate 90 degrees counterclockwise (negative Y-axis rotation)
			transform.rotation = target.rotation * Quaternion.Euler(0, -90, 0);
		}
	}


	private void ActivateOnHand(FVRViveHand hand)
	{
		if (!this.m_isActive)
		{
			this.m_isActive = true;
			this.m_currentHand = hand;
			Canvas.gameObject.SetActive(true);
			this.PositionWristMenu();
		}
	}

	public void Deactivate()
	{
		if (this.m_isActive)
		{
			this.m_isActive = false;
			Canvas.gameObject.SetActive(false);
		}
	}
}
} // namespace NGA
