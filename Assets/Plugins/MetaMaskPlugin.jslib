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

  ////////////// WALLET CONNECT //////////////

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

  SendTransaction: function (toPtr, dataPtr, valuePtr, objectNamePtr, successCallbackPtr, errorCallbackPtr) {
    var to = UTF8ToString(toPtr);
    var data = UTF8ToString(dataPtr);
    var value = UTF8ToString(valuePtr);
    var objectName = UTF8ToString(objectNamePtr);
    var successCallback = UTF8ToString(successCallbackPtr);
    var errorCallback = UTF8ToString(errorCallbackPtr);

    if (typeof window.ethereum !== "undefined") {
      window.ethereum.request({ method: 'eth_requestAccounts' })
        .then(function(accounts) {
          var from = accounts[0];
          var txParams = {
            to: to,
            from: from,
            data: data,
            value: value
          };

          window.ethereum.request({
            method: "eth_sendTransaction",
            params: [txParams]
          })
          .then(function(txHash) {
            console.log("Transaction sent:", txHash);
            if (typeof window !== 'undefined' && window.unityReady) {
              SendMessage(objectName, successCallback, txHash);
            }
          })
          .catch(function(error) {
            console.error("Transaction failed:", error);
            if (typeof window !== 'undefined' && window.unityReady) {
              SendMessage(objectName, errorCallback, error.message);
            }
          });
        })
        .catch(function(error) {
          console.error("Failed to request accounts:", error);
          if (typeof window !== 'undefined' && window.unityReady) {
            SendMessage(objectName, errorCallback, error.message);
          }
        });
    } else {
      console.warn("MetaMask not found");
      if (typeof window !== 'undefined' && window.unityReady) {
        SendMessage(objectName, errorCallback, "MetaMask not found");
      }
    }
  },
  ////////////// USERUSER CONTRACT //////////////

  // Initiates a transfer by calling the UserUser contract's initiateTransfer(address,uint256,string)
  InitiateTransferJS: function(toPtr, amountPtr, messagePtr, contractAddressPtr) {
    var to = UTF8ToString(toPtr).trim();
    var amount = UTF8ToString(amountPtr);
    var message = UTF8ToString(messagePtr);
    var contractAddress = UTF8ToString(contractAddressPtr).trim();
    var target = window.walletLoginObjectName || "WalletLogin";

    // Validate addresses
    function isValidAddress(addr) {
      return /^0x[a-fA-F0-9]{40}$/.test(addr);
    }
    if (!isValidAddress(to)) {
      var errMsg = 'Invalid recipient address: [' + to + '] (length: ' + to.length + ')';
      console.warn(errMsg);
      if (typeof window !== 'undefined' && window.unityReady) {
        SendMessage(target, 'OnBalanceErrorStatic', errMsg);
      }
      return;
    }
    if (!isValidAddress(contractAddress)) {
      var errMsg = 'Invalid contract address: [' + contractAddress + '] (length: ' + contractAddress.length + ')';
      console.warn(errMsg);
      if (typeof window !== 'undefined' && window.unityReady) {
        SendMessage(target, 'OnBalanceErrorStatic', errMsg);
      }
      return;
    }

    if (typeof window.ethereum === 'undefined') {
      if (typeof window !== 'undefined' && window.unityReady) {
        SendMessage(target, 'OnBalanceErrorStatic', 'MetaMask not found');
      }
      return;
    }

    function encodeInitiateTransfer(to, amount, message) {
      var toClean = to.replace(/^0x/, '');
      var toPadded = toClean.padStart(64, '0');
      var amountHex = BigInt(amount).toString(16);
      var amountPadded = amountHex.padStart(64, '0');
      var offset = '0000000000000000000000000000000000000000000000000000000000000060';
      var msgBytes = new TextEncoder().encode(message);
      var msgLen = msgBytes.length.toString(16).padStart(64, '0');
      var msgHex = Array.from(msgBytes).map(b => b.toString(16).padStart(2, '0')).join('');
      var padLen = Math.ceil(msgHex.length / 64) * 64;
      var msgHexPadded = msgHex.padEnd(padLen, '0');
      var selector = '0xa1bc28e7';
      var data = selector + toPadded + amountPadded + offset + msgLen + msgHexPadded;
      return data;
    }

    var data = encodeInitiateTransfer(to, amount, message);

    // Request accounts and send transaction with 'from' field
    window.ethereum.request({ method: 'eth_requestAccounts' })
      .then(function(accounts) {
        var from = accounts[0];
        window.ethereum.request({
          method: 'eth_sendTransaction',
          params: [{
            from: from,
            to: contractAddress,
            value: '0x' + BigInt(amount).toString(16),
            data: data
            // Let MetaMask estimate gas
          }]
        })
        .then(function(txHash) {
          if (typeof window !== 'undefined' && window.unityReady) {
            // Send tx hash to a specific callback, not the balance one.
            SendMessage(target, 'OnInitiateTransferSubmittedStatic', txHash);
          }

          // Poll for the transaction receipt to confirm it was mined.
          function pollForReceipt(currentTxHash, userAddress, attempt = 0) {
            const maxAttempts = 60; // Poll for ~2 minutes (60 * 2s)
            const pollInterval = 2000; // 2 seconds

            if (attempt >= maxAttempts) {
              console.warn('InitiateTransferJS: Max attempts for polling receipt for tx:', currentTxHash);
              SendMessage(target, 'OnBalanceErrorStatic', 'Transaction polling timed out for ' + currentTxHash);
              return;
            }

            window.ethereum.request({ method: 'eth_getTransactionReceipt', params: [currentTxHash] })
              .then(function(receipt) {
                if (receipt && receipt.blockNumber) {
                  if (receipt.status === '0x1') { // Success
                    SendMessage(target, 'OnInitiateTransferConfirmedStatic', currentTxHash);
                    
                    // After confirmation, fetch the new balance.
                    window.ethereum.request({
                      method: 'eth_getBalance',
                      params: [userAddress, 'latest']
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
                  } else { // Transaction failed
                    console.error('InitiateTransferJS: Transaction failed with status', receipt.status, 'for tx:', currentTxHash);
                    SendMessage(target, 'OnBalanceErrorStatic', 'Transaction failed: ' + currentTxHash);
                  }
                } else { // Not mined yet, poll again
                  setTimeout(() => pollForReceipt(currentTxHash, userAddress, attempt + 1), pollInterval);
                }
              })
              .catch(function(receiptError) {
                console.error('InitiateTransferJS: Error fetching transaction receipt for', currentTxHash, receiptError);
                setTimeout(() => pollForReceipt(currentTxHash, userAddress, attempt + 1), pollInterval);
              });
          }
          pollForReceipt(txHash, from); // 'from' is the sender's address
        })
        .catch(function(error) {
          if (typeof window !== 'undefined' && window.unityReady) {
            console.error('MetaMask eth_sendTransaction error object:', error); // Log the full error object
            var errorMessage = error.message || "Unknown error";
            if (error.data && error.data.message) {
              errorMessage += " | Node error: " + error.data.message;
            } else if (error.data) {
              errorMessage += " | Error data: " + JSON.stringify(error.data);
            }
            SendMessage(target, 'OnBalanceErrorStatic', errorMessage);
          }
        });
      })
      .catch(function(error) { // catch for eth_requestAccounts
        if (typeof window !== 'undefined' && window.unityReady) {
          console.error('MetaMask eth_requestAccounts error object:', error); // Log the full error object
          SendMessage(target, 'OnBalanceErrorStatic', error.message);
        }
      });
  },

  SignTransferJS: function(contractAddressPtr) {
    var contractAddress = UTF8ToString(contractAddressPtr).trim();
    var target = window.walletLoginObjectName || "WalletLogin";

    if (!(/^0x[a-fA-F0-9]{40}$/.test(contractAddress))) {
      var errMsg = 'Invalid contract address for SignTransferJS: [' + contractAddress + ']';
      console.warn(errMsg);
      if (typeof window !== 'undefined' && window.unityReady) {
        SendMessage(target, 'OnSignTransferErrorStatic', errMsg);
      }
      return;
    }

    if (typeof window.ethereum === 'undefined') {
      if (typeof window !== 'undefined' && window.unityReady) {
        SendMessage(target, 'OnSignTransferErrorStatic', 'MetaMask not found');
      }
      return;
    }

    var functionSelector = '0x0ff308ef'; // Function selector for signTransfer()
    var data = functionSelector;

    window.ethereum.request({ method: 'eth_requestAccounts' })
      .then(function(accounts) {
        var from = accounts[0];
        window.ethereum.request({
          method: 'eth_sendTransaction',
          params: [{
            from: from,
            to: contractAddress,
            data: data
          }]
        })
        .then(function(txHash) {
          // Send the transaction hash to Unity
          SendMessage(target, 'OnSignTransferSubmittedStatic', txHash);

          function pollForReceiptAndFetchMessage(currentTxHash, signerAddress, attempt = 0) {
            const maxAttempts = 60; // Poll for ~2 minutes (60 * 2s)
            const pollInterval = 2000; // 2 seconds

            if (attempt >= maxAttempts) {
              console.warn('SignTransferJS: Max attempts reached for polling receipt for tx:', currentTxHash);
              SendMessage(target, 'OnSignTransferErrorStatic', 'Transaction polling timed out for ' + currentTxHash);
              return;
            }

            window.ethereum.request({ method: 'eth_getTransactionReceipt', params: [currentTxHash] })
              .then(function(receipt) {
                if (receipt && receipt.blockNumber) {
                  if (receipt.status === '0x1') { // Success
                    SendMessage(target, 'OnSignTransferConfirmedStatic', currentTxHash);

                    // After confirmation, fetch the new balance for the recipient.
                    window.ethereum.request({
                      method: 'eth_getBalance',
                      params: [signerAddress, 'latest']
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

                    const eventSignature = '0x3cbe146da1765a56590fc311c3b29069bdb5edcbda2fed678afe2c48cdaa75cd'; // Should stay the same if contract is not modified 
                    const paddedSignerAddress = '0x' + signerAddress.slice(2).padStart(64, '0').toLowerCase();

                    window.ethereum.request({
                      method: 'eth_getLogs',
                      params: [{
                        fromBlock: receipt.blockNumber,
                        toBlock: receipt.blockNumber,
                        address: contractAddress,
                        topics: [eventSignature, paddedSignerAddress]
                      }]
                    })
                    .then(function(logs) {
                      var foundMessage = null;
                      if (logs && logs.length > 0) {
                        var log = logs[0];
                        let logData = log.data.startsWith('0x') ? log.data.substring(2) : log.data;
                        const messageLengthHex = logData.substring(128, 128 + 64);
                        const messageLengthInBytes = parseInt(messageLengthHex, 16);

                        if (messageLengthInBytes > 0) {
                            const messageStart = 128 + 64;
                            const messageEnd = messageStart + (messageLengthInBytes * 2);
                            const messageContentHex = logData.substring(messageStart, messageEnd);

                            let decodedMsg = '';
                            for (let j = 0; j < messageContentHex.length; j += 2) {
                              decodedMsg += String.fromCharCode(parseInt(messageContentHex.substring(j, j + 2), 16));
                            }
                            foundMessage = decodedMsg;
                        } else {
                            foundMessage = ""; // Empty message
                        }
                      }

                      if (foundMessage !== null) {
                        SendMessage(target, 'OnSignedMessageReceivedStatic', foundMessage);
                      } else {
                        console.warn('SignTransferJS: No matching log found for tx:', currentTxHash, 'with signer', signerAddress);
                        SendMessage(target, 'OnSignedMessageErrorStatic', 'Event log not found for ' + currentTxHash);
                      }
                    })
                    .catch(function(logError) {
                      console.error('SignTransferJS: Error fetching event logs:', logError);
                      SendMessage(target, 'OnSignedMessageErrorStatic', 'Error fetching event logs: ' + (logError.message || logError));
                    });
                  } else { // Transaction failed
                    console.error('SignTransferJS: Transaction failed with status', receipt.status, 'for tx:', currentTxHash);
                    SendMessage(target, 'OnSignTransferErrorStatic', 'Transaction failed: ' + currentTxHash + ' with status ' + receipt.status);
                  }
                } else { // Not mined yet, poll again
                  setTimeout(() => pollForReceiptAndFetchMessage(currentTxHash, signerAddress, attempt + 1), pollInterval);
                }
              })
              .catch(function(receiptError) {
                console.error('SignTransferJS: Error fetching transaction receipt for', currentTxHash, 'Attempt:', attempt, receiptError);
                // Continue polling unless max attempts, as it might be a temporary network issue
                setTimeout(() => pollForReceiptAndFetchMessage(currentTxHash, signerAddress, attempt + 1), pollInterval);
              });
          }
          pollForReceiptAndFetchMessage(txHash, from); // 'from' is the signer's address
        })
        .catch(function(error) { // Catch for eth_sendTransaction
          console.error('SignTransferJS: Error sending transaction:', error);
          if (typeof window !== 'undefined' && window.unityReady) {
            SendMessage(target, 'OnSignTransferErrorStatic', error.message || 'Failed to send transaction');
          }
        });
      })
      .catch(function(error) { // Catch for eth_requestAccounts
        console.error('SignTransferJS: Error requesting accounts:', error);
        if (typeof window !== 'undefined' && window.unityReady) {
          SendMessage(target, 'OnSignTransferErrorStatic', error.message || 'Failed to request accounts');
        }
      });
  }
});