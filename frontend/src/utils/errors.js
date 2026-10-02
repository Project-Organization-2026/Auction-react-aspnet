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
