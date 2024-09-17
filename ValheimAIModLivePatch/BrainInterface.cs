using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimAIModLoader
{
    public partial class ValheimAIModLivePatch : BaseUnityPlugin
    {
        float lastSentToBrainTime = 0f;
        private void SendRecordingToBrain()
        {
            if (IsRecording)
            {
                instance.StopRecording();
            }

            //GameObject[] allNpcs = FindPlayerNPC();
            if (PlayerNPC)
            {
                MonsterAI monsterAIcomponent = PlayerNPC.GetComponent<MonsterAI>();
                HumanoidNPC humanoidComponent = PlayerNPC.GetComponent<HumanoidNPC>();

                //Debug.Log("BrainSendInstruction");
                BrainSendInstruction(PlayerNPC);
                instance.lastSentToBrainTime = Time.time;

                AddChatTalk(humanoidComponent, "NPC", "...");
            }
        }

        private async Task SendLogToBrain()
        {
            if (logEntries.Count <= 0) return;

            if (!LogToBrain.Value) return;

            StringBuilder res = new StringBuilder();
            foreach (string entry in logEntries)
            {
                res.AppendLine(entry);
            }

            var jObject = new JsonObject
            {
                ["player_id"] = GetPlayerSteamID(),
                ["timestamp"] = DateTime.Now.ToString(),
                ["log_string"] = res.ToString(),
            };

            // Create a new WebClient
            WebClient webClient = new WebClient();
            webClient.Headers.Add("Content-Type", "application/json");
            webClient.UploadStringCompleted += OnSendLogToBrainCompleted;


            try
            {
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var uploadTask = webClient.UploadStringTaskAsync(new Uri($"{GetBrainAPIAddress()}/log_valheim"), IndentJson(jObject.ToString()));

                var completedTask = await Task.WhenAny(uploadTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    webClient.CancelAsync();
                    throw new TimeoutException("Request timed out after 10 seconds");
                }

                await uploadTask; // Ensure any exceptions are thrown
                LogInfo("Successfully logged to brain!");
            }
            catch (WebException ex)
            {
                LogError($"Error connecting to server/log: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                LogError($"Request timed out /log: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogError($"An error occurred /log: {ex.Message}");
            }



            // Send the POST request
            /*webClient.UploadStringAsync(new System.Uri($"{GetBrainAPIAddress()}/log_valheim"), jObject.ToString());
            webClient.UploadStringCompleted += OnSendLogToBrainCompleted;*/


            /*string FilePath = Path.Combine(UnityEngine.Application.persistentDataPath, "lastlog.json");
            LogInfo($"Saving temp log to {FilePath}");

            File.WriteAllText(FilePath, jObject.ToString());*/

            logEntries.Clear();
        }

        private void OnSendLogToBrainCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                LogInfo("Logged to brain completed!");

            }
            else
            {
                LogError("Sending log to brain failed: " + e.Error.Message);
            }
        }

        public void BrainSynthesizeAudio(string text, string voice)
        {
            using (WebClient client = new WebClient())
            {
                // Construct the URL with query parameters
                string url = $"{GetBrainAPIAddress()}/synthesize_audio?text={Uri.EscapeDataString(text)}&voice={Uri.EscapeDataString(voice)}&player_id={GetPlayerSteamID()}";

                client.DownloadStringCompleted += OnBrainSynthesizeAudioResponse;
                client.DownloadStringAsync(new Uri(url));
            }
        }

        private void OnBrainSynthesizeAudioResponse(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                LogError($"Synthesize Audio Download failed: {e.Error.Message}");
                return;
            }

            try
            {
                JsonObject responseObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(e.Result);
                string audio_file_id = responseObject["audio_file_id"].ToString();
                string text = responseObject["text"].ToString();
                HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();

                //AddChatTalk(npc, "NPC", text);
                DownloadAudioFile(audio_file_id);
            }
            catch (Exception ex)
            {
                LogError($"Failed to parse Synthesize Audio Download JSON: {ex.Message}");
            }

            instance.previewVoiceButton.SetActive(true);
            SetPreviewVoiceButtonState(instance.previewVoiceButtonComp, true, 1);
        }

        private void BrainSendPeriodicUpdate(GameObject npc)
        {
            string jsonData = GetJSONForBrain(npc, false);

            WebClient webClient = new WebClient();
            webClient.Headers.Add("Content-Type", "application/json");

            webClient.UploadStringAsync(new System.Uri($"{GetBrainAPIAddress()}/instruct_agent"), jsonData);
            webClient.UploadStringCompleted += OnBrainSendPeriodicUpdateResponse;
        }

        private void OnBrainSendPeriodicUpdateResponse(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                string responseJson = e.Result;

                // Parse the response JSON using SimpleJSON's DeserializeObject
                JsonObject responseObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(responseJson);
                string audioFileId = responseObject["agent_text_response_audio_file_id"].ToString();
                string agent_text_response = responseObject["agent_text_response"].ToString();
                string player_instruction_transcription = responseObject["player_instruction_transcription"].ToString();

                // Get the agent_commands array
                JsonArray agentCommands = responseObject["agent_commands"] as JsonArray;

                // Check if agent_commands array exists and has at least one element
                if (agentCommands != null && agentCommands.Count > 0)
                {
                    for (int i = 0; i < agentCommands.Count; i++)
                    {
                        JsonObject commandObject = agentCommands[i] as JsonObject;

                        if (!(commandObject.ContainsKey("action") && commandObject.ContainsKey("category")))
                        {
                            HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();
                            AddChatTalk(npc, "NPC", agent_text_response);

                            LogError("Agent command response from brain was incomplete. Command's Action or Category is missing!");
                            continue;
                        }

                        string action = commandObject["action"].ToString();
                        string category = commandObject["category"].ToString();

                        string[] parameters = { };
                        string p = "";

                        if (commandObject.ContainsKey("parameters"))
                        {
                            JsonArray jsonparams = commandObject["parameters"] as JsonArray;
                            if (jsonparams != null && jsonparams.Count > 0)
                            {
                                p = jsonparams[0].ToString();
                            }
                        }

                        foreach (string pa in parameters)
                        {
                            LogError($"param {pa}");
                        }

                        Debug.Log("NEW COMMAND: Category: " + category + ". Action : " + action + ". Parameters: " + parameters);
                        ProcessNPCCommand(category, action, p, agent_text_response);

                        Sprite defaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

                        AddItemToScrollBox(TaskListScrollBox, $"{action} {category} ({p})", defaultSprite, 0);
                    }
                }
                else
                {
                    HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();
                    AddChatTalk(npc, "NPC", agent_text_response);
                    Debug.Log("No agent commands found.");
                }

                Debug.Log("Brain periodic update response: " + responseJson);
            }
            else
            {
                Debug.LogError("Request failed: " + e.Error.Message);
            }
        }

        private async Task BrainSendInstruction(GameObject npc, bool Voice = true)
        {
            string jsonData = GetJSONForBrain(npc, Voice);

            //Debug.Log("jsonData\n " + jsonData);

            // Create a new WebClient
            WebClient webClient = new WebClient();
            webClient.Headers.Add("Content-Type", "application/json");
            webClient.UploadStringCompleted += OnBrainSendInstructionResponse;

            try
            {
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var uploadTask = webClient.UploadStringTaskAsync(new Uri($"{GetBrainAPIAddress()}/instruct_agent"), jsonData);

                var completedTask = await Task.WhenAny(uploadTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    webClient.CancelAsync();
                    throw new TimeoutException("Request timed out after 10 seconds");

                }

                await uploadTask; // Ensure any exceptions are thrown
            }
            catch (WebException ex)
            {
                LogError($"Brain Send Instruction | Error connecting to server: {ex.Message}");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Error connecting to Thrall server!");
            }
            catch (TimeoutException ex)
            {
                LogError($"Brain Send Instruction | Request timed out: {ex.Message}");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Timeout error while connecting to Thrall server!");
            }
            catch (Exception ex)
            {
                LogError($"Brain Send Instruction | An error occurred: {ex.Message}");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "An error occurred while trying to connect to Thrall server!");
            }



            // Send the POST request
            /*webClient.UploadStringAsync(new System.Uri($"{BrainAPIAddress.Value}/instruct_agent"), jsonData);
            webClient.UploadStringCompleted += OnBrainSendInstructionResponse;*/
        }

        private void OnBrainSendInstructionResponse(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                string responseJson = IndentJson(e.Result);

                // Parse the response JSON using SimpleJSON's DeserializeObject
                JsonObject responseObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(responseJson);
                string audioFileId = responseObject["agent_text_response_audio_file_id"].ToString();
                string agent_text_response = responseObject["agent_text_response"].ToString().TrimStart('\n');
                string player_instruction_transcription = responseObject["player_instruction_transcription"].ToString();

                //Debug.Log("Response from brain");

                LogInfo("Full response from brain: " + responseJson);
                LogMessage("You said: " + player_instruction_transcription);
                LogMessage("NPC said: " + agent_text_response);

                // Get the agent_commands array
                JsonArray agentCommands = responseObject["agent_commands"] as JsonArray;

                // Check if agent_commands array exists and has at least one element
                if (PlayerNPC && agentCommands != null && agentCommands.Count > 0)
                {
                    for (int i = 0; i < agentCommands.Count; i++)
                    {
                        JsonObject commandObject = agentCommands[i] as JsonObject;
                        HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();

                        AddChatTalk(Player.m_localPlayer, "Player", player_instruction_transcription);
                        AddChatTalk(npc, "NPC", agent_text_response);

                        if (!(commandObject.ContainsKey("action") && commandObject.ContainsKey("category")))
                        {
                            LogError("Agent command response from brain was incomplete. Command's Action or Category is missing!");
                            continue;
                        }

                        string action = commandObject["action"].ToString();
                        string category = commandObject["category"].ToString();

                        string[] parameters = { };

                        string parametersString = "";

                        if (commandObject.ContainsKey("parameters"))
                        {
                            JsonArray jsonparams = commandObject["parameters"] as JsonArray;
                            parameters = jsonparams.Select(x => x.ToString()).ToArray();
                        }

                        foreach (string s in parameters)
                        {
                            parametersString += $"{s}, ";
                        }

                        LogInfo($"NEW COMMAND: {category} {action} {parametersString}");
                        if (category == "Inventory")
                            ProcessNPCCommand(category, action, parameters.Length > 0 ? parameters[0] : "", agent_text_response);

                        Sprite defaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

                        if (category == "Harvesting")
                        {
                            string ResourceName = null;
                            int ResourceQuantity = 0;
                            string ResourceNode = null;

                            if (parameters.Length > 0)
                            {
                                for (int x = 0; i < parameters.Length; x++)
                                {
                                    if (x > 2) break;

                                    if (x == 0)
                                    {
                                        ResourceName = parameters[x];
                                    }
                                    else if (int.TryParse(parameters[x].Trim('\''), out int quantity))
                                    {
                                        ResourceQuantity = quantity;
                                    }
                                    else
                                    {
                                        ResourceNode = parameters[x];
                                    }
                                }

                                if (ResourceQuantity < 1)
                                {
                                    ResourceQuantity = 1;
                                }



                                HarvestAction harvestAction = new HarvestAction();
                                harvestAction.humanoidNPC = npc;
                                harvestAction.ResourceName = ResourceName;
                                harvestAction.RequiredAmount = ResourceQuantity;
                                harvestAction.OriginalRequiredAmount = ResourceQuantity;
                                //harvestAction.RequiredAmount = ResourceQuantity + CountItemsInInventory(npc.m_inventory, ResourceName);
                                instance.commandManager.AddCommand(harvestAction);
                            }
                            else
                            {
                                LogError("Brain gave Harvesting command but didn't give 3 parameters");
                            }
                        }
                        else if (category == "Patrol")
                        {
                            PatrolAction patrolAction = new PatrolAction();
                            patrolAction.humanoidNPC = npc;
                            patrolAction.patrol_position = Player.m_localPlayer.transform.position;
                            patrolAction.patrol_radius = 15;
                            instance.commandManager.AddCommand(patrolAction);
                        }
                        else if (category == "Combat")
                        {
                            string TargetName = null;
                            string WeaponName = null;
                            int TargetQty = 1;

                            if (parameters.Length > 0)
                            {
                                for (int x = 0; i < parameters.Length; x++)
                                {
                                    if (x > 2) break;

                                    if (x == 0)
                                    {
                                        TargetName = parameters[x];
                                    }
                                    else if (int.TryParse(parameters[x].Trim('\''), out int quantity))
                                    {
                                        TargetQty = quantity;
                                    }
                                    else
                                    {
                                        WeaponName = parameters[x];
                                    }
                                }

                                if (TargetQty < 1)
                                {
                                    TargetQty = 1;
                                }

                                AttackAction attackAction = new AttackAction();
                                attackAction.humanoidNPC = npc;
                                attackAction.TargetName = TargetName;
                                attackAction.TargetQuantity = TargetQty;
                                attackAction.OriginalTargetQuantity = TargetQty;
                                instance.commandManager.AddCommand(attackAction);
                            }
                            else
                            {
                                LogError("Brain gave Combat command but didn't give a parameters");
                            }


                        }
                        if (category == "Follow")
                        {
                            FollowAction followAction = new FollowAction();
                            followAction.humanoidNPC = npc;
                            instance.commandManager.AddCommand(followAction);
                        }


                    }

                    RefreshTaskList();
                }
                else
                {
                    HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();
                    AddChatTalk(Player.m_localPlayer, "Player", player_instruction_transcription);
                    AddChatTalk(npc, "NPC", agent_text_response);
                    LogInfo("No agent commands sent by brain.");
                }

                DownloadAudioFile(audioFileId);
            }
            else
            {
                LogError("Request failed: " + e.Error.Message);
            }
        }

        private void DownloadAudioFile(string audioFileId)
        {
            // Create a new WebClient for downloading the audio file
            WebClient webClient = new WebClient();

            // Download the audio file asynchronously
            webClient.DownloadDataAsync(new System.Uri($"{GetBrainAPIAddress()}/get_audio_file?audio_file_id={audioFileId}&player_id={GetPlayerSteamID()}"));
            webClient.DownloadDataCompleted += OnAudioFileDownloaded;
        }

        private void OnAudioFileDownloaded(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                // Save the audio file to disk
                System.IO.File.WriteAllBytes(npcDialogueRawAudioPath, e.Result);
                //Debug.Log("Audio file downloaded to: " + npcDialogueRawAudioPath);

                if (instance.lastSentToBrainTime > 0)
                    LogInfo("Brain response time: " + (Time.time - instance.lastSentToBrainTime));

                PlayWavFile(npcDialogueRawAudioPath);

            }
            else if (e.Error is WebException webException && webException.Status == WebExceptionStatus.ProtocolError && ((HttpWebResponse)webException.Response).StatusCode == HttpStatusCode.NotFound)
            {
                LogError("Audio file does not exist.");
            }
            else
            {
                LogError("Download failed: " + e.Error.Message);
            }
        }

        private void ProcessNPCCommand(string category, string action, string parameter, string agent_text_response)
        {
            Player localPlayer = Player.m_localPlayer;

            //string firstParameter = parameters.Length > 0 ? parameters[0] : "NULL";

            /*if (category == "Follow")
            {
                if (action == "Start")
                {
                    instance.Follow_Start(localPlayer.gameObject, agent_text_response);
                }
                else if (action == "Stop")
                {
                    instance.Follow_Stop(agent_text_response);
                }
            }

            else if (category == "Combat")
            {
                if (action == "StartAttacking")
                {
                    instance.Combat_StartAttacking(parameter, agent_text_response);
                }
                else if (action == "Sneak")
                {
                    instance.Combat_StartSneakAttacking(null, agent_text_response);
                }
                else if (action == "Defend")
                {
                    instance.Combat_StartDefending(null, agent_text_response);
                }
                else if (action == "StopAttacking")
                {
                    instance.Combat_StopAttacking(agent_text_response);
                }
            }*/

            //else if (category == "Inventory")
            if (category == "Inventory")
            {
                if (action == "DropAll")
                {
                    instance.Inventory_DropAll(agent_text_response);
                }
                else if (action == "DropItem")
                {
                    instance.Inventory_DropItem(parameter, agent_text_response);
                }
                else if (action == "EquipItem")
                {
                    instance.Inventory_EquipItem(parameter, agent_text_response);
                }
                else if (action == "PickupItem")
                {
                }
            }
            /*else if (category == "Harvesting")
            {
                if (action == "Start")
                {
                    //Debug.Log($"harvesting start {parameter}");
                    instance.Harvesting_Start(parameter, agent_text_response);
                }
                else if (action == "Stop")
                {
                    instance.Harvesting_Stop(agent_text_response);
                }
            }
            else if (category == "Patrol")
            {
                if (action == "Start")
                {
                    instance.Patrol_Start(agent_text_response);
                }
                else if (action == "Stop")
                {
                    instance.Patrol_Stop(agent_text_response);
                }
            }*/
            /*else
            {
                Debug.Log($"ProcessNPCCommand failed {category} {action}");
            }*/
        }
    }
}
