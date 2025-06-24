mergeInto(LibraryManager.library, {
  walletLoginObjectName: "WalletLogin",

  RegisterWalletLoginObject: function(namePtr) {
    var name = UTF8ToString(namePtr);
    window.walletLoginObjectName = name;
  },

  OnUnityReady: function() {
    if (typeof window !== 'undefined') {
      window.unityReady = true;
    }
  },

  CallContractFunction: function(toPtr, dataPtr) {
    var to = UTF8ToString(toPtr);
    var data = UTF8ToString(dataPtr);

    if (typeof window.ethereum !== "undefined") {
      var callParams = { to: to, data: data };

      window.ethereum.request({
        method: "eth_call",
        params: [callParams, "latest"]
      })
      .then(function(result) {
        console.log("Contract call result:", result);
        SendMessage("DoorBridgeObject", "HandleDoorAccessResult", result); // aina ohjataan yhteen GameObjectiin
      })
      .catch(function(error) {
        console.error("Contract call failed:", error);
      });
    } else {
      console.warn("MetaMask not found");
    }
  },

  ConnectWallet: function () {
    var target = window.walletLoginObjectName || "WalletLogin";
    if (typeof window.ethereum !== 'undefined') {
      window.ethereum.request({ method: 'eth_requestAccounts' })
        .then(function(accounts) {
          var address = accounts[0];
          if (typeof window !== 'undefined' && window.unityReady) {
            SendMessage(target, 'OnWalletConnectedStatic', address);
          }
        })
        .catch(function(error) {
          if (typeof window !== 'undefined' && window.unityReady) {
            SendMessage(target, 'OnWalletConnectionFailedStatic', error.message);
          }
        });
    } else {
      if (typeof window !== 'undefined' && window.unityReady) {
        SendMessage(target, 'OnWalletConnectionFailedStatic', 'MetaMask not found');
      }
    }
  },

  FetchBalanceJS: function(addressPtr) {
    var target = window.walletLoginObjectName || "WalletLogin";
    if (typeof window.ethereum !== 'undefined') {
      var address = UTF8ToString(addressPtr);
      window.ethereum.request({
        method: 'eth_getBalance',
        params: [address, 'latest']
      })
      .then(function(result) {
        if (typeof window !== 'undefined' && window.unityReady) {
          SendMessage(target, 'OnBalanceReceivedStatic', result);
        }
      })
      .catch(function(error) {
        if (typeof window !== 'undefined' && window.unityReady) {
          SendMessage(target, 'OnBalanceErrorStatic', error.message);
        }
      });
    }
  },

  SendTransaction: function (toPtr, dataPtr, valuePtr) {
    var to = UTF8ToString(toPtr);
    var data = UTF8ToString(dataPtr);
    var value = UTF8ToString(valuePtr);

    if (typeof window.ethereum !== "undefined") {
      var txParams = {
        to: to,
        from: ethereum.selectedAddress,
        data: data,
        value: value
      };

      window.ethereum.request({
        method: "eth_sendTransaction",
        params: [txParams]
      })
      .then(function(txHash) {
        console.log("Transaction sent:", txHash);
      })
      .catch(function(error) {
        console.error("Transaction failed:", error);
      });
    } else {
      console.warn("MetaMask not found");
    }
  }
});
