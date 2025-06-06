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
  }
});
