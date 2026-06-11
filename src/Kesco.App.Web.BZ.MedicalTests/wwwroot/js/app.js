// ── Kesco application helpers ────────────────────────────────────────────────

/**
 * Устанавливает фокус на DOM-элемент как только он становится видимым.
 * Используется вместо Task.Delay + FocusAsync в Blazor-компонентах, чтобы не
 * зависеть от конкретной длительности CSS-анимации (например, открытия диалога).
 *
 * Механизм: через requestAnimationFrame опрашивает элемент до тех пор, пока
 * offsetParent !== null (элемент виден и не скрыт через display:none),
 * после чего вызывает .focus().
 *
 * @param {HTMLElement} element — ссылка на DOM-элемент (ElementReference из Blazor)
 */
window.kescoFocusElement = function (element) {
    if (!element) return;

    const tryFocus = () => {
        if (element.offsetParent !== null || element.offsetWidth > 0) {
            element.focus();
        } else {
            requestAnimationFrame(tryFocus);
        }
    };

    requestAnimationFrame(tryFocus);
};
