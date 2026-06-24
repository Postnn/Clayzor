// ── Kesco Column Settings Dialog — Sortable drag-and-drop ────────────────────
//
// jQuery UI Sortable-подобный механизм.
// Важно: JS не удаляет и не заменяет DOM-узлы Blazor (replaceChild/removeChild
// на Blazor-элементах конфликтует с reconciliation и вызывает insertBefore crash).
//
// Принцип:
//   - Источник скрывается через visibility:hidden (остаётся в Blazor-дереве)
//   - Placeholder — отдельный div, вставляется через insertBefore (Blazor его не знает)
//   - Ghost следует за курсором (position:fixed, вне контейнера)
//   - После mouseup: cleanup() убирает ghost+placeholder, вызывает OnJsDrop
//   - Blazor получает (srcIdx, targetIdx), переставляет _items, делает StateHasChanged
//
// targetIdx — индекс вставки в оригинальном N-элементном массиве Blazor

window.kescoColumnSettings = (function () {

    return {
        init: function (container, dotnetRef) {

            var dragging    = false;
            var sourceChip  = null;   // оригинальный DOM-узел (скрыт)
            var sourceIdx   = -1;     // data-col-idx источника
            var ghost       = null;   // клон, следует за курсором
            var placeholder = null;   // маркер позиции вставки
            var grabOffsetX = 0;
            var grabOffsetY = 0;
            var chipH       = 0;
            var chipW       = 0;
            var HYST        = 8;      // мёртвая зона (px) у центра чипа

            // ── helpers ──────────────────────────────────────────────────────

            /** Видимые чипы Blazor (без источника, без placeholder). */
            function getChips() {
                return Array.from(container.querySelectorAll('[data-col-idx]'))
                    .filter(function (c) { return c !== sourceChip; });
            }

            function createGhost(chip) {
                var g = chip.cloneNode(true);
                g.className      = 'column-settings-chip column-settings-ghost';
                g.style.width    = chipW + 'px';
                g.style.height   = chipH + 'px';
                g.style.position = 'fixed';
                g.style.zIndex   = '9999';
                g.style.pointerEvents = 'none';
                g.style.margin   = '0';
                document.body.appendChild(g);
                return g;
            }

            function createPlaceholder() {
                var p = document.createElement('div');
                p.className    = 'column-settings-placeholder';
                p.style.height = chipH + 'px';
                // Пометим чтобы не попал в getChips()
                p.dataset.placeholder = '1';
                return p;
            }

            function moveGhost(x, y) {
                if (!ghost) return;
                ghost.style.left = (x - grabOffsetX) + 'px';
                ghost.style.top  = (y - grabOffsetY) + 'px';
            }

            /**
             * Перемещает placeholder в нужную позицию по курсору.
             * Placeholder вставляется insertBefore — Blazor его не знает, конфликта нет.
             */
            function movePlaceholder(clientY) {
                if (!container || !placeholder) return;
                var chips = getChips();
                for (var i = 0; i < chips.length; i++) {
                    var chip = chips[i];
                    var rect = chip.getBoundingClientRect();
                    var mid  = rect.top + rect.height / 2;
                    if (clientY < mid - HYST) {
                        if (!chip.parentNode) return;
                        if (placeholder.nextSibling !== chip)
                            container.insertBefore(placeholder, chip);
                        return;
                    }
                }
                // Курсор ниже всех — в конец контейнера
                if (container && container.lastChild !== placeholder)
                    container.appendChild(placeholder);
            }

            /**
             * Вычисляет targetIdx для Blazor.
             * Считаем количество чипов (data-col-idx) перед placeholder,
             * не считая сам источник (он скрыт но присутствует в DOM).
             * Это даёт позицию вставки в массиве после RemoveAt(sourceIdx).
             */
            function getTargetIdx() {
                if (!placeholder.parentNode) return sourceIdx;
                var allNodes = Array.from(container.childNodes);
                var phPos    = allNodes.indexOf(placeholder);
                var count    = 0;
                for (var i = 0; i < phPos; i++) {
                    var n = allNodes[i];
                    if (n === sourceChip) continue; // не считаем источник
                    if (n.hasAttribute && n.hasAttribute('data-col-idx')) count++;
                }
                return count;
            }

            function cleanup() {
                if (ghost) { ghost.remove(); ghost = null; }
                if (placeholder && placeholder.parentNode)
                    placeholder.parentNode.removeChild(placeholder);
                placeholder = null;
                if (sourceChip) {
                    sourceChip.style.display = '';
                    sourceChip = null;
                }
                dragging  = false;
                sourceIdx = -1;
                document.removeEventListener('mousemove', onMouseMove);
                document.removeEventListener('mouseup',   onMouseUp);
            }

            // ── Обработчики ──────────────────────────────────────────────────

            function onMouseMove(e) {
                if (!dragging) return;
                moveGhost(e.clientX, e.clientY);
                // Вставляем placeholder в DOM при первом движении
                if (!placeholder.parentNode)
                    container.appendChild(placeholder);
                movePlaceholder(e.clientY);
            }

            function onMouseUp(e) {
                if (!dragging) return;
                var targetIdx = getTargetIdx();
                var src       = sourceIdx;
                cleanup();
                dotnetRef.invokeMethodAsync('OnJsDrop', src, targetIdx);
            }

            function onKeyDown(e) {
                if (e.key === 'Escape' && dragging) cleanup();
            }

            // ── Инициализация ────────────────────────────────────────────────

            container.addEventListener('mousedown', function (e) {
                if (e.button !== 0) return;
                var chip = e.target.closest('[data-col-idx]');
                if (!chip) return;
                if (e.target.closest('input, button, .mud-switch-base, .mud-button-root')) return;

                e.preventDefault();

                var rect    = chip.getBoundingClientRect();
                chipW       = rect.width;
                chipH       = rect.height;
                grabOffsetX = e.clientX - rect.left;
                grabOffsetY = e.clientY - rect.top;
                sourceIdx   = parseInt(chip.dataset.colIdx, 10);
                sourceChip  = chip;

                ghost       = createGhost(chip);
                placeholder = createPlaceholder();

                // Убираем источник — место сразу схлопывается.
                // Placeholder НЕ вставляем здесь — он появится при первом mousemove
                // над другим чипом. Пока placeholder не в DOM, список компактный.
                chip.style.display = 'none';
                void container.offsetHeight; // форсируем reflow

                moveGhost(e.clientX, e.clientY);
                dragging = true;

                document.addEventListener('mousemove', onMouseMove);
                document.addEventListener('mouseup',   onMouseUp);
            });

            document.addEventListener('keydown', onKeyDown);
        }
    };

})();
