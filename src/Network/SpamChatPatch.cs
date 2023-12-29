using HarmonyLib;
using Hazel;
using UnityEngine;
using System.Text.RegularExpressions;

namespace MalumMenu;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
public static class Network_SpamTextPostfix
{
    public static string spamText;
    private static float lastChatTime = 0f;
    private static float chatDelay = 0.5f; // Delay between messages in seconds

    // Prefix patch of PlayerControl.RpcSendChat
    public static bool Prefix(string chatText, PlayerControl __instance)
    {
        if (CheatSettings.spamChat)
        {
            chatText = Regex.Replace(chatText, "<.*?>", string.Empty);
            if (string.IsNullOrWhiteSpace(chatText))
            {
                return false;
            }

            spamText = chatText;
            return false; // Skip the original method when the cheat is active
        }

        return true; // Send a normal chat message if the cheat is not active
    }

    public static void Update()
    {
        if (CheatSettings.spamChat && spamText != null && Time.time - lastChatTime >= chatDelay)
        {
            lastChatTime = Time.time;
            SendSpamChat();
        }
    }

    private static void SendSpamChat()
    {
        var HostData = AmongUsClient.Instance.GetHost();
        if (HostData != null && !HostData.Character.Data.Disconnected)
        {
            foreach (var sender in PlayerControl.AllPlayerControls)
            {
                foreach (var recipient in PlayerControl.AllPlayerControls)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(sender.NetId, (byte)RpcCalls.SendChat, SendOption.None, AmongUsClient.Instance.GetClientIdFromCharacter(recipient.Data.Object));
                    writer.Write(spamText);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
public static class Network_SpamChatPostfix
{
    public static void Postfix(PlayerPhysics __instance)
    {
        Network_SpamTextPostfix.Update();
        if (!CheatSettings.spamChat)
        {
            Network_SpamTextPostfix.spamText = null;
        }
    }
}
