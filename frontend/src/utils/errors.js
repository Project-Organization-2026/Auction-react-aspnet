/**
 * Extracts a clean, human-readable error message from an API error response.
 * Handles RFC 9110 / 7807 ProblemDetails, ASP.NET Core ModelState errors,
 * custom { message: "..." } objects, and plain strings.
 */
export function getErrorMessage(err, defaultMessage = "An error occurred. Please try again.") {
  if (!err) return defaultMessage;

  const data = err.response?.data;
  if (!data) {
    return err.message || defaultMessage;
  }

  // 1. ValidationProblemDetails: { errors: { Field: ["error1", "error2"] } }
  if (data.errors && typeof data.errors === "object") {
    const messages = [];
    for (const [, fieldErrors] of Object.entries(data.errors)) {
      if (Array.isArray(fieldErrors)) {
        fieldErrors.forEach((msg) => {
          if (typeof msg === "string" && msg.trim()) {
            messages.push(msg.trim());
          }
        });
      } else if (typeof fieldErrors === "string" && fieldErrors.trim()) {
        messages.push(fieldErrors.trim());
      }
    }
    if (messages.length > 0) {
      return messages.join("\n");
    }
  }

  // 2. Custom { message: "..." }
  if (typeof data.message === "string" && data.message.trim()) {
    return data.message.trim();
  }

  // 3. Direct string response (e.g. return BadRequest("User already exists"))
  if (typeof data === "string" && data.trim()) {
    if (!data.startsWith("<!DOCTYPE") && !data.startsWith("<html")) {
      return data.trim();
    }
  }

  // 4. Problem title: { title: "..." }
  if (typeof data.title === "string" && data.title.trim()) {
    return data.title.trim();
  }

  return defaultMessage;
}

/**
 * Extracts a user-friendly message from Web3 / Ethers.js / RPC errors.
 * Formats INSUFFICIENT_FUNDS, gas estimation failures, user rejections,
 * smart contract custom errors, and RPC connection drops into clear English.
 */
export function formatWeb3Error(err, defaultMessage = "Failed to execute on-chain transaction.") {
  if (!err) return defaultMessage;

  const rawMessage = String(err.message || "");
  const code = err.code || err.info?.error?.code || err.error?.code;
  const innerMsg = err.info?.error?.message || err.error?.message || "";
  const combined = `${rawMessage} ${innerMsg}`.toLowerCase();

  // 1. Insufficient funds for gas or bid value
  if (
    code === "INSUFFICIENT_FUNDS" ||
    code === -32000 ||
    combined.includes("insufficient funds")
  ) {
    return "Insufficient ETH in your wallet to cover the bid amount and network gas fee. Please top up your wallet with test ETH.";
  }

  // 2. User rejected transaction in MetaMask
  if (
    code === "ACTION_REJECTED" ||
    code === 4001 ||
    combined.includes("user rejected") ||
    combined.includes("user denied")
  ) {
    return "Transaction was cancelled in MetaMask.";
  }

  // 3. Smart contract revert / custom errors
  if (code === "CALL_EXCEPTION" || combined.includes("execution reverted")) {
    if (combined.includes("bidnothighenough")) {
      return "Your ETH bid must be strictly higher than the current highest bid.";
    }
    if (combined.includes("auctioalreadyended") || combined.includes("auctionnotyetended")) {
      return "This auction has already ended.";
    }
    if (combined.includes("transferfailed")) {
      return "Smart contract failed to transfer ETH.";
    }
    if (err.reason) {
      return `Smart contract error: ${err.reason}`;
    }
    return "Transaction reverted by smart contract.";
  }

  // 4. Local node / RPC connection failure
  if (
    combined.includes("failed to fetch") ||
    combined.includes("network error") ||
    combined.includes("could not detect network")
  ) {
    return "Could not connect to the Ethereum node. Please verify that your local node (Ganache / Hardhat) is running.";
  }

  // 5. Short message fallback
  if (err.shortMessage) {
    return err.shortMessage;
  }

  // If message contains long debug data like (transaction={...}), strip it
  const parenIdx = rawMessage.indexOf("(");
  if (parenIdx > 0 && parenIdx < 100) {
    return rawMessage.substring(0, parenIdx).trim();
  }

  return rawMessage.length > 160 ? `${rawMessage.slice(0, 160)}...` : rawMessage || defaultMessage;
}
