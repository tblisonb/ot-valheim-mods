using System.Collections.Generic;
using HarmonyLib;

namespace OtBuildOrientation
{
    // Optional server-side gate. Players without the mod don't see built pieces turn until they
    // reload the area (see RotatePatches.cs), so a dedicated server running the mod can turn away
    // clients that don't have it. Installed only on clients, or on a server with RequireOnClients
    // off, none of this does anything.
    //
    // Every modded client sends a hello RPC on the connection as soon as it opens, right after
    // vanilla's ServerHandshake call. The server only gets the client's PeerInfo (the step that
    // actually lets it in) after answering that handshake, so the hello has always arrived first on
    // the same ordered connection. A vanilla server has no handler for it and ignores it, as ZRpc does
    // with any unknown method.
    internal static class ServerCheck
    {
        private const string HelloRpc = "OtBuildOrientation_Hello";

        // Bumped only if the multiplayer side changes incompatibly; the plugin version alone can
        // change freely between client and server.
        private const int Protocol = 1;

        private static readonly HashSet<ZRpc> Greeted = new HashSet<ZRpc>();

        internal static void OnNewConnection(ZNet net, ZNetPeer peer)
        {
            if (net.IsServer())
            {
                peer.m_rpc.Register<int>(HelloRpc, (rpc, protocol) =>
                {
                    if (protocol == Protocol)
                    {
                        Greeted.Add(rpc);
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"{peer.m_socket.GetEndPointString()} has an incompatible {Plugin.PluginName} (protocol {protocol}, mine {Protocol}).");
                    }
                });
            }
            else
            {
                peer.m_rpc.Invoke(HelloRpc, Protocol);
            }
        }

        // Rejected the way vanilla rejects a wrong game version: an "Error" RPC with
        // ConnectionStatus.ErrorVersion, which the client shows as an incompatible-version
        // message and disconnects on. There's no way to send the client a custom reason.
        internal static bool Admit(ZNet net, ZRpc rpc)
        {
            if (!net.IsServer() || !net.IsDedicated() || !Plugin.RequireOnClients.Value || Greeted.Contains(rpc))
            {
                return true;
            }
            Plugin.Log.LogInfo($"Turning away {rpc.GetSocket().GetEndPointString()}:it doesn't have {Plugin.PluginName} installed (RequireOnClients is on).");
            rpc.Invoke("Error", (int)ZNet.ConnectionStatus.ErrorVersion);
            return false;
        }

        internal static void Forget(ZNetPeer peer)
        {
            if (peer != null)
            {
                Greeted.Remove(peer.m_rpc);
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class OnNewConnectionPatch
    {
        private static void Postfix(ZNet __instance, ZNetPeer peer)
        {
            ServerCheck.OnNewConnection(__instance, peer);
        }
    }

    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    internal static class PeerInfoPatch
    {
        private static bool Prefix(ZNet __instance, ZRpc rpc)
        {
            return ServerCheck.Admit(__instance, rpc);
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
    internal static class DisconnectPatch
    {
        private static void Prefix(ZNetPeer peer)
        {
            ServerCheck.Forget(peer);
        }
    }
}
