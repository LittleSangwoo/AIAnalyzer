/* ==========================================
 * ГЛОБАЛЬНЫЕ ФУНКЦИИ ИНТЕРФЕЙСА
 * ========================================== */

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
    const bsToast = new bootstrap.Toast(toastElement, { delay: 4000 }); 
    bsToast.show();
    
    toastElement.addEventListener('hidden.bs.toast', () => toastElement.remove());
}

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
        
        const iconEl = document.getElementById('confirmIcon');
        const yesBtn = document.getElementById('confirmYes');
        
        if (isDanger) {
            iconEl.className = 'bi bi-exclamation-octagon text-danger mb-3';
            yesBtn.className = 'btn btn-danger btn-rounded px-4 shadow-sm';
        } else {
            iconEl.className = 'bi bi-question-circle text-primary mb-3';
            yesBtn.className = 'btn btn-primary btn-rounded px-4 shadow-sm';
        }

        const newYesBtn = yesBtn.cloneNode(true);
        yesBtn.parentNode.replaceChild(newYesBtn, yesBtn);
        
        let isConfirmed = false;
        
        newYesBtn.addEventListener('click', () => {
            isConfirmed = true;
            modal.hide();
            resolve(true);
        });

        modalEl.addEventListener('hidden.bs.modal', function handler() {
            modalEl.removeEventListener('hidden.bs.modal', handler);
            if (!isConfirmed) {
                resolve(false);
            }
        });
        
        modal.show();
    });
}

/* ==========================================
 * ЛОГИКА СТРАНИЦЫ НАСТРОЕК
 * ========================================== */

function toggleAuthFields() {
    const isLocal = document.getElementById("pIsLocal")?.checked;
    const authContainer = document.getElementById("authContainer");
    if (authContainer) {
        authContainer.classList.toggle("hidden", isLocal);
    }
}

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

/* ==========================================
 * ЛОГИКА СТРАНИЦЫ АНАЛИТИКИ
 * ========================================== */

let selectedFiles = [];
const AI_HISTORY_KEY = 'aianalyzer_chat_history';
const LABEL_MAP = {
    rewrite:         '✏️ Улучшить текст',
    course_redesign: '📘 Аудит курса',
    test_redesign:   '🎛 Формат теста',
    custom:          '💬 Свой вопрос',
};

function saveHistoryItem(label, markdownText) {
    let history = JSON.parse(localStorage.getItem(AI_HISTORY_KEY) || '[]');
    history.push({ label, markdownText });
    localStorage.setItem(AI_HISTORY_KEY, JSON.stringify(history));
}

function loadHistory() {
    const history = JSON.parse(localStorage.getItem(AI_HISTORY_KEY) || '[]');
    history.forEach(item => appendBubble(item.label, item.markdownText, false));
}

window.removeFile = function(index) {
    selectedFiles.splice(index, 1);
    renderFileList();
};

function renderFileList() {
    const container = document.getElementById('fileListContainer');
    if (!container) return;
    container.innerHTML = '';
    selectedFiles.forEach((file, index) => {
        const item = document.createElement('div');
        item.className = 'd-flex justify-content-between align-items-center p-2 rounded small shadow-sm light-theme-border';
        item.style.cssText = 'border: 1px solid var(--app-border); background-color: var(--app-surface-hover);';
        item.innerHTML = `
            <span class="text-truncate" style="max-width: 85%;" title="${file.name}">
                <i class="bi bi-file-earmark-text me-2 text-primary"></i>${file.name}
            </span>
            <button type="button" class="btn-close" style="font-size: 0.6rem;" onclick="removeFile(${index})"></button>
        `;
        container.appendChild(item);
    });
}

async function loadModelDropdown() {
    try {
        const response = await fetch('/Settings/GetProviders');
        if (!response.ok) throw new Error('Ошибка сети');

        const providers = await response.json();
        const dropdown = document.getElementById('dynamicModelList');
        if (!dropdown) return;

        dropdown.innerHTML = '';

        if (providers.length === 0) {
            dropdown.innerHTML = '<li><span class="dropdown-item text-muted">Нет настроенных ИИ. Добавьте их в настройках.</span></li>';
            document.getElementById('selectedModelText').innerText = "Не выбрано";
            return;
        }

        providers.forEach((p, index) => {
            const li = document.createElement('li');
            const a = document.createElement('a');
            
            const isActive = index === 0 ? ' active fw-bold' : '';
            a.className = `dropdown-item model-select-item${isActive}`;
            a.href = '#';
            a.setAttribute('data-val', p.name); 
            
            if (p.isLocal) {
                a.classList.add('text-success', 'fw-bold');
                a.innerHTML = `<i class="bi bi-pc-display me-2"></i>${p.name}`;
            } else {
                a.innerHTML = p.name;
            }

            a.addEventListener('click', function(e) {
                e.preventDefault();
                document.querySelectorAll('.model-select-item').forEach(el => el.classList.remove('active', 'fw-bold'));
                this.classList.add('active', 'fw-bold');
                
                document.getElementById('modelSelect').value = this.getAttribute('data-val');
                document.getElementById('selectedModelText').innerText = this.innerText;
            });

            li.appendChild(a);
            dropdown.appendChild(li);

            if (index === 0) {
                document.getElementById('modelSelect').value = p.name;
                document.getElementById('selectedModelText').innerText = a.innerText;
            }
        });
    } catch (error) {
        console.error("Не удалось загрузить список ИИ:", error);
    }
}

async function clearHistory() {
    const isConfirmed = await confirmAction('Очистка чата', 'Вы уверены, что хотите удалить всю историю диалогов?', true);
    if (isConfirmed) {
        localStorage.removeItem(AI_HISTORY_KEY);
        const out = document.getElementById('ai-output');
        out.innerHTML = '<div class="ai-welcome"><i class="bi bi-stars me-2 text-primary"></i>История очищена. Готов к новым запросам.</div>';
        showToast("История успешно очищена", "success");
    }
}

function showThinking(text) {
    const out = document.getElementById('ai-output');
    const existing = out.querySelector('.ai-thinking');
    if (existing) existing.remove();

    const el = document.createElement('div');
    el.className = 'ai-thinking';
    el.innerHTML = `<div class="spinner-grow spinner-grow-sm text-primary" role="status"></div><span>${text}</span>`;
    out.appendChild(el);
    out.scrollTop = out.scrollHeight;
    return el;
}

function appendBubble(label, markdownText, save = true) {
    const out = document.getElementById('ai-output');
    const welcome = out.querySelector('.ai-welcome');
    if (welcome) welcome.remove();

    const bubble = document.createElement('div');
    bubble.className = 'ai-chat-bubble';

    const parsed = (typeof marked !== 'undefined')
        ? marked.parse(markdownText)
        : markdownText.replace(/\n/g, '<br>');

    bubble.innerHTML = `
        <div class="bubble-meta">
            <span class="bubble-label">${label}</span>
            <button class="bubble-copy-btn" onclick="copyBubble(this)" title="Копировать ответ">
                <i class="bi bi-copy"></i> Копировать
            </button>
        </div>
        <div class="ai-markdown-content">${parsed}</div>
    `;

    out.appendChild(bubble);
    out.scrollTop = out.scrollHeight;

    if (save) {
        saveHistoryItem(label, markdownText);
    }
}

function copyBubble(btn) {
    const bubble = btn.closest('.ai-chat-bubble');
    const contentDiv = bubble.querySelector('.ai-markdown-content');
    const textToCopy = contentDiv.innerText;

    navigator.clipboard.writeText(textToCopy).then(() => {
        const originalHtml = btn.innerHTML;
        btn.innerHTML = '<i class="bi bi-check-lg"></i> Скопировано';
        btn.classList.add('copied');

        setTimeout(() => {
            btn.innerHTML = originalHtml;
            btn.classList.remove('copied');
        }, 2000);
    }).catch(err => {
        console.error('Ошибка копирования:', err);
    });
}

async function downloadExcelReport() {
    if (!window.loadedQuestions || window.loadedQuestions.length === 0) {
        showToast("Нет данных для выгрузки.", "warning"); 
        return;
    }

    const aiResponses = [];
    const aiBubbles = document.querySelectorAll('.ai-markdown-content');
    aiBubbles.forEach(bubble => {
        aiResponses.push(bubble.innerText);
    });

    const visibleIds = Array.from(document.querySelectorAll('#resultsBody tr.data-row'))
        .filter(tr => tr.style.display !== 'none')
        .map(tr => tr.querySelector('.question-cb').value);
            
    const requestData = {
        questions: window.loadedQuestions, 
        aiRecommendations: aiResponses
    };

    const btn = document.querySelector('button[onclick="downloadExcelReport()"]');
    const originalHtml = btn.innerHTML;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span> Формируем...';
    btn.disabled = true;

    try {
        const response = await fetch('/api/analyzer/export', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });
        if (!response.ok) throw new Error("Ошибка при генерации Excel на сервере.");

        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Отчет_ИИ_Аналитики_${new Date().toLocaleDateString('ru-RU')}.xlsx`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);
        
        showToast("Отчет успешно сформирован и скачан!", "success");

    } catch (error) {
        console.error(error);
        showToast("Ошибка при скачивании файла: " + error.message, "error");
    } finally {
        btn.innerHTML = originalHtml;
        btn.disabled = false;
    }
}

async function processAiResponse(response, thinkingEl, label) {
    if (thinkingEl) thinkingEl.remove();
    if (!response.ok) throw new Error(await response.text());

    let rawText = await response.text();
    let parsedText = rawText;
    try { parsedText = JSON.parse(rawText); } catch(e) {}

    appendBubble(label, parsedText);
}

function filterResults() {
    const search = document.getElementById('searchInput').value.toLowerCase();
    const showRed    = document.getElementById('filterRed').checked;
    const showYellow = document.getElementById('filterYellow').checked;
    const showGreen  = document.getElementById('filterGreen').checked;

    document.querySelectorAll('#resultsBody tr.data-row').forEach(row => {
        const matchText = row.getAttribute('data-qtext').includes(search) ||
                          row.getAttribute('data-qid').includes(search);
        const zone = row.getAttribute('data-qzone');
        const matchZone = (zone === 'red' && showRed) ||
                          (zone === 'yellow' && showYellow) ||
                          (zone === 'green' && showGreen);

        if (matchText && matchZone) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
            row.querySelector('.question-cb').checked = false;
        }
    });
    updateSelectAllCb();
}

function updateSelectAllCb() {
    const visible = Array.from(document.querySelectorAll('.question-cb'))
        .filter(cb => cb.closest('tr').style.display !== 'none');
    const checked = visible.filter(cb => cb.checked);
    document.getElementById('selectAllCb').checked = visible.length > 0 && visible.length === checked.length;
}

function renderTable(questions) {
    const tbody = document.getElementById('resultsBody');
    tbody.innerHTML = '';
    document.getElementById('selectAllCb').checked = false;

    if (!questions || questions.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-5">Выборка пуста.</td></tr>`;
        return;
    }

    questions.forEach(q => {
        const zoneVal  = q.zone          !== undefined ? q.zone          : q.Zone;
        const errs     = q.errorsCount   !== undefined ? q.errorsCount   : q.ErrorsCount;
        const corrs    = q.correctCount  !== undefined ? q.correctCount  : q.CorrectCount;
        const qId      = q.questionId    || q.QuestionId;
        const qText    = String(q.questionText || q.QuestionText || '');
        const percent  = q.errorPercentage !== undefined ? q.errorPercentage : q.ErrorPercentage;

        let rowClass = 'zone-row-green', zoneStr = 'green';
        if (zoneVal === 2 || String(zoneVal).toLowerCase() === 'red')    { rowClass = 'zone-row-red';    zoneStr = 'red'; }
        else if (zoneVal === 1 || String(zoneVal).toLowerCase() === 'yellow') { rowClass = 'zone-row-yellow'; zoneStr = 'yellow'; }

        const tr = document.createElement('tr');
        tr.className = rowClass + ' data-row border-bottom';
        tr.setAttribute('data-qtext', qText.toLowerCase());
        tr.setAttribute('data-qid', String(qId).toLowerCase());
        tr.setAttribute('data-qzone', zoneStr);
        tr.innerHTML = `
            <td class="text-center align-middle border-end border-light">
                <input class="form-check-input elegant-checkbox question-cb border-secondary" type="checkbox" value="${qId}">
            </td>
            <td class="fw-bold opacity-75">${qId}</td>
            <td class="py-3 pe-3 border-end border-light">${qText}</td>
            <td class="text-success fw-bold text-center">${corrs}</td>
            <td class="text-danger fw-bold text-center">${errs}</td>
            <td class="text-center fw-bold opacity-75">${percent}%</td>
        `;
        tr.querySelector('.question-cb').addEventListener('change', updateSelectAllCb);
        tbody.appendChild(tr);
    });
}

async function sendToAi(promptType) {
    if (!window.loadedQuestions || window.loadedQuestions.length === 0) {
        showToast("Сначала загрузите матрицу данных.", "warning"); 
        return;
    }

    const checkedIds = Array.from(document.querySelectorAll('.question-cb:checked')).map(cb => cb.value);
    let targetQuestions = [];

    if (promptType === 'course_redesign' || promptType === 'test_redesign') {
        targetQuestions = [...window.loadedQuestions].sort((a, b) => {
            const errA = a.errorPercentage !== undefined ? a.errorPercentage : a.ErrorPercentage;
            const errB = b.errorPercentage !== undefined ? b.errorPercentage : b.ErrorPercentage;
            return errB - errA;
        });
    } else {
        if (checkedIds.length > 0) {
            targetQuestions = window.loadedQuestions.filter(q => checkedIds.includes(String(q.questionId || q.QuestionId)));
        } else {
            targetQuestions = window.loadedQuestions.filter(q => {
                const z = q.zone !== undefined ? q.zone : q.Zone;
                return z === 2 || String(z).toLowerCase() === 'red';
            });
        }
    }

    if (targetQuestions.length === 0) {
        appendBubble(LABEL_MAP[promptType] || promptType, 'Нет вопросов для анализа. Выберите вопросы галочками.');
        return;
    }

    const thinkingEl = showThinking(`ИИ изучает ${targetQuestions.length} вопросов…`);

    try {
        const [response] = await Promise.all([
            fetch('/api/ai/recommendation', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    redZoneQuestions: targetQuestions,
                    promptType: promptType,
                    modelProvider: document.getElementById('modelSelect').value,
                    apiKey: ""
                })
            }),
            new Promise(r => setTimeout(r, 900))
        ]);
        await processAiResponse(response, thinkingEl, LABEL_MAP[promptType] || promptType);
    } catch (error) {
        if (thinkingEl) thinkingEl.remove();
        appendBubble('⚠️ Ошибка', error.message);
    }
}

async function sendCustomPrompt() {
    const prompt = document.getElementById('customPrompt').value.trim();
    if (!prompt) { 
        showToast("Введите текст запроса.", "warning"); 
        return; 
    }

    const thinkingEl = showThinking('Обдумываю ответ…');

    try {
        const [response] = await Promise.all([
            fetch('/api/ai/custom', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    prompt: prompt,
                    modelProvider: document.getElementById('modelSelect').value,
                    apiKey: ""
                })
            }),
            new Promise(r => setTimeout(r, 900))
        ]);
        await processAiResponse(response, thinkingEl, `💬 ${prompt.length > 50 ? prompt.slice(0, 50) + '…' : prompt}`);
        document.getElementById('customPrompt').value = '';
    } catch (error) {
        if (thinkingEl) thinkingEl.remove();
        appendBubble('⚠️ Ошибка', error.message);
    }
}

/* ==========================================
 * ЗАПУСК И ИНИЦИАЛИЗАЦИЯ СОБЫТИЙ 
 * ========================================== */

document.addEventListener("DOMContentLoaded", function () {
    // 1. Блок для страницы НАСТРОЕК
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

    // 2. Блок для страницы АНАЛИТИКИ
    const uploadForm = document.getElementById('uploadForm');
    if (uploadForm) {
        if (typeof marked !== 'undefined') {
            marked.use({ breaks: true, gfm: true });
        }

        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => new bootstrap.Tooltip(el));

        loadModelDropdown();
        loadHistory();

        document.getElementById('answersFiles').addEventListener('change', function(e) {
            selectedFiles = selectedFiles.concat(Array.from(e.target.files));
            renderFileList();
            e.target.value = '';
        });

        document.getElementById('selectAllCb').addEventListener('change', function(e) {
            document.querySelectorAll('.question-cb').forEach(cb => {
                if (cb.closest('tr').style.display !== 'none') cb.checked = e.target.checked;
            });
        });

        uploadForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            const btn = document.getElementById('uploadBtn');

            if (selectedFiles.length === 0) {
                showToast("Пожалуйста, добавьте файлы для загрузки.", "warning"); 
                return;
            }

            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Обработка...';
            btn.disabled = true;

            const formData = new FormData();
            selectedFiles.forEach(f => formData.append('answersFiles', f));

            try {
                const response = await fetch('/api/analyzer/upload', { method: 'POST', body: formData });
                if (!response.ok) throw new Error('Сбой при парсинге файлов');

                const data = await response.json();
                const questionsList = data.questions || data.Questions || [];
                
                window.currentQuestionsData = questionsList;
                window.loadedQuestions = questionsList;
                renderTable(questionsList);

                document.getElementById('searchInput').value = '';
                document.getElementById('filterRed').checked = true;
                document.getElementById('filterYellow').checked = true;
                document.getElementById('filterGreen').checked = true;
                filterResults();

                selectedFiles = [];
                renderFileList();
                
                showToast(`Успешно загружено вопросов: ${questionsList.length}`, 'success');

            } catch (error) {
                showToast("Ошибка системы: " + error.message, 'error');
            } finally {
                btn.innerHTML = 'Загрузить данные';
                btn.disabled = false;
            }
        });
    }
});