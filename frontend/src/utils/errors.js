/**
 * Витягує зрозумілий та чистий текст помилки з відповіді сервера або об'єкта помилки.
 * Підтримує RFC 9110 / 7807 ProblemDetails, ASP.NET Core ModelState errors,
 * кастомні { message: "..." } та звичайні рядки.
 */
export function getErrorMessage(err, defaultMessage = "Виникла помилка") {
  if (!err) return defaultMessage;

  const data = err.response?.data;
  if (!data) {
    return err.message || defaultMessage;
  }

  // 1. Обробка словника помилок валідації (ValidationProblemDetails): { errors: { Field: ["error1", "error2"] } }
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

  // 2. Обробка об'єкта з полем { message: "..." }
  if (typeof data.message === "string" && data.message.trim()) {
    return data.message.trim();
  }

  // 3. Прямий рядок від сервера (наприклад, BadRequest("Користувач вже існує"))
  if (typeof data === "string" && data.trim()) {
    if (!data.startsWith("<!DOCTYPE") && !data.startsWith("<html")) {
      return data.trim();
    }
  }

  // 4. Заголовок помилки { title: "..." }
  if (typeof data.title === "string" && data.title.trim()) {
    return data.title.trim();
  }

  return defaultMessage;
}
