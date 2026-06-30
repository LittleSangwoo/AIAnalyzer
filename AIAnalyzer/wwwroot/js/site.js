
/**
 * Вызов лаконичного всплывающего уведомления (Toast)
 * @param {string} message - Текст уведомления
 * @param {string} type - Тип: 'info', 'success', 'error', 'warning'
 */
function showToast(message, type = 'info') {
    const container = document.getElementById('toast-container');
    if (!container) {
        console.warn('Контейнер для уведомлений (toast-container) не найден.');
        return;
    }
    
    let icon = 'bi-info-circle';
    let colorClass = 'text-primary';

    if (type === 'success') { icon = 'bi-check-circle-fill'; colorClass = 'text-success'; }
    if (type === 'error') { icon = 'bi-x-circle-fill'; colorClass = 'text-danger'; }
    if (type === 'warning') { icon = 'bi-exclamation-triangle-fill'; colorClass = 'text-warning'; }

    const toastId = 'toast-' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center border-0 shadow" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex p-1">
                <div class="toast-body d-flex align-items-center gap-2 fw-medium fs-6">
                    <i class="bi ${icon} ${colorClass} fs-5"></i>
                    ${message}
                </div>
                <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Закрыть"></button>
            </div>
        </div>
    `;
    
    container.insertAdjacentHTML('beforeend', toastHtml);
    
    const toastElement = document.getElementById(toastId);
    // Авто-закрытие через 4 секунды (4000 мс)
    const bsToast = new bootstrap.Toast(toastElement, { delay: 4000 }); 
    bsToast.show();
    
    // Удаляем элемент из DOM после его полного скрытия, чтобы не засорять страницу
    toastElement.addEventListener('hidden.bs.toast', () => toastElement.remove());
}

/**
 * Вызов красивого модального окна с подтверждением действия
 * @param {string} title - Заголовок окна (например, "Удаление")
 * @param {string} message - Текст сообщения
 * @param {boolean} isDanger - Если true, кнопка будет красной (акцент на опасном действии)
 * @returns {Promise<boolean>} - Возвращает true (нажали "Да") или false (Отмена)
 */
function confirmAction(title, message, isDanger = false) {
    return new Promise((resolve) => {
        const modalEl = document.getElementById('confirmModal');
        if (!modalEl) {
            console.error('Модальное окно confirmModal не найдено.');
            resolve(false);
            return;
        }

        const modal = new bootstrap.Modal(modalEl);
        
        document.getElementById('confirmTitle').innerText = title;
        document.getElementById('confirmMessage').innerText = message;
        
        // Смена стилей, если действие опасное (например, удаление данных)
        const iconEl = document.getElementById('confirmIcon');
        const yesBtn = document.getElementById('confirmYes');
        
        if (isDanger) {
            iconEl.className = 'bi bi-exclamation-octagon text-danger mb-3';
            yesBtn.className = 'btn btn-danger btn-rounded px-4 shadow-sm';
        } else {
            iconEl.className = 'bi bi-question-circle text-primary mb-3';
            yesBtn.className = 'btn btn-primary btn-rounded px-4 shadow-sm';
        }

        // Клонируем кнопку "Да", чтобы сбросить обработчики от предыдущих вызовов
        const newYesBtn = yesBtn.cloneNode(true);
        yesBtn.parentNode.replaceChild(newYesBtn, yesBtn);
        
        let isConfirmed = false;
        
        // Обработка нажатия "Да"
        newYesBtn.addEventListener('click', () => {
            isConfirmed = true;
            modal.hide();
            resolve(true);
        });

        // Обработка закрытия окна (нажатие "Отмена", клик мимо окна или крестик)
        modalEl.addEventListener('hidden.bs.modal', function handler() {
            modalEl.removeEventListener('hidden.bs.modal', handler);
            if (!isConfirmed) {
                resolve(false);
            }
        });
        
        modal.show();
    });
}
function toggleAuthFields() {
    const isLocal = document.getElementById("pIsLocal")?.checked;
    const authContainer = document.getElementById("authContainer");
    if (authContainer) {
        authContainer.classList.toggle("hidden", isLocal);
    }
}

// Переключение типа авторизации (GigaChat vs Standard)
function toggleAuthType() {
    const type = document.getElementById("authType")?.value;
    const scopeGroup = document.getElementById("scopeGroup");
    const apiKeyLabel = document.getElementById("apiKeyLabel");
    const apiKeyHelp = document.getElementById("apiKeyHelp");

    if (!scopeGroup || !apiKeyLabel || !apiKeyHelp) return;

    if (type === "gigachat") {
        scopeGroup.classList.remove("hidden");
        apiKeyLabel.innerText = "Авторизационные данные (Base64)";
        apiKeyHelp.classList.remove("hidden");
    } else {
        scopeGroup.classList.add("hidden");
        apiKeyLabel.innerText = "Токен авторизации (API Key)";
        apiKeyHelp.classList.add("hidden");
    }
}

// Загрузка списка провайдеров
async function loadProviders() {
    const container = document.getElementById("providersList");
    if (!container) return;

    try {
        const response = await fetch('/Settings/GetProviders');
        const providers = await response.json();
        container.innerHTML = "";

        if (providers.length === 0) {
            container.innerHTML = `
                <div class="text-center py-5 text-muted opacity-50">
                    <i class="bi bi-database-exclamation fs-1 d-block mb-3"></i>
                    Список подключений пуст. Добавьте первый шлюз слева.
                </div>`;
            return;
        }

        providers.forEach(p => {
            const badge = p.isLocal
                ? `<span class="status-badge badge-local"><i class="bi bi-pc-display me-1"></i> Local</span>`
                : `<span class="status-badge badge-cloud"><i class="bi bi-cloud-fill me-1"></i> Cloud</span>`;

            const apiKeyHtml = p.apiKey 
                ? `<div class="mt-1" style="word-break: break-all;"><i class="bi bi-key me-1 opacity-75"></i>${p.apiKey}</div>` 
                : '';
            const scopeHtml = p.scope 
                ? `<div class="mt-1" style="word-break: break-all;"><i class="bi bi-shield-lock me-1 opacity-75"></i>${p.scope}</div>` 
                : '';
            
            container.innerHTML += `
                <div class="provider-item-card align-items-start">
                    <div class="d-flex align-items-start gap-3 w-100 pe-3">
                        <div class="rounded-circle p-2 d-flex align-items-center justify-content-center flex-shrink-0" style="background: rgba(139, 92, 246, 0.1); width: 40px; height: 40px;">
                            <i class="bi bi-hdd-network" style="color:#8b5cf6; font-size:1.2rem;"></i>
                        </div>
                        <div class="flex-grow-1">
                            <div class="fw-bold mb-2">${p.name}</div>
                            <div class="text-muted small" style="line-height: 1.6;">
                                <div><i class="bi bi-box me-1 opacity-75"></i>${p.modelName}</div>
                                <div class="mt-1"><i class="bi bi-link-45deg me-1 opacity-75"></i><span style="word-break: break-all;">${p.apiUrl}</span></div>
                                ${apiKeyHtml}
                                ${scopeHtml}
                            </div>
                        </div>
                    </div>
                    <div class="d-flex flex-column align-items-end gap-2 flex-shrink-0">
                        ${badge}
                        <button type="button" class="btn btn-action-icon mt-auto" title="Удалить" onclick="deleteProvider('${p.id}')">
                            <i class="bi bi-trash3-fill"></i>
                        </button>
                    </div>
                </div>
            `;
        });
    } catch (error) {
        console.error("Ошибка загрузки:", error);
    }
}

// Удаление провайдера
async function deleteProvider(id) {
    const isConfirmed = await confirmAction('Удаление провайдера', 'Удалить эту конфигурацию?', true);
    if (!isConfirmed) return;

    try {
        const response = await fetch(`/Settings/DeleteProvider?id=${id}`, { method: 'DELETE' });
        if (response.ok) {
            await loadProviders();
            showToast("Конфигурация удалена", "success");
        } else {
            showToast("Ошибка при удалении", "error");
        }
    } catch (err) { showToast("Ошибка сети", "error"); }
}

// Инициализация событий на странице
document.addEventListener("DOMContentLoaded", () => {
    const providerForm = document.getElementById("providerForm");
    if (providerForm) {
        toggleAuthFields();
        loadProviders();

        providerForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            const isLocal = document.getElementById("pIsLocal").checked;
            
            const payload = {
                name: document.getElementById("pName").value,
                isLocal: isLocal,
                modelName: document.getElementById("pModelName").value,
                apiUrl: document.getElementById("pApiUrl").value,
                apiKey: !isLocal ? document.getElementById("pApiKey").value.trim() : "",
                scope: (!isLocal && document.getElementById("authType").value === "gigachat") 
                       ? document.getElementById("pScope").value.trim() : null
            };

            try {
                const response = await fetch('/Settings/AddProvider', {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify(payload)
                });

                if (response.ok) {
                    providerForm.reset();
                    toggleAuthFields();
                    await loadProviders();
                    showToast("Провайдер добавлен!", "success");
                } else {
                    showToast("Ошибка добавления провайдера", "error");
                }
            } catch (err) {
                showToast("Ошибка соединения", "error");
            }
        });
    }
});