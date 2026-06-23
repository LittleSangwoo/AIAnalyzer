document.addEventListener("DOMContentLoaded", function () {
    const uploadForm = document.getElementById('uploadForm');

    if (uploadForm) {
        uploadForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const etalonFile = document.getElementById('etalonFile').files[0];
            const answersFile = document.getElementById('answersFile').files[0];
            const statusDiv = document.getElementById('uploadStatus');
            const btn = document.getElementById('uploadBtn');

            if (!etalonFile || !answersFile) return;

            // Индикация загрузки
            statusDiv.classList.remove('d-none', 'alert-danger', 'alert-success');
            statusDiv.classList.add('alert-warning');
            statusDiv.textContent = 'Обработка данных...';
            btn.disabled = true;

            const formData = new FormData();
            formData.append('etalonFile', etalonFile);
            formData.append('answersFile', answersFile);

            try {
                // Отправляем файлы в AnalyzerController
                const response = await fetch('/api/analyzer/upload', {
                    method: 'POST',
                    body: formData
                });

                if (!response.ok) throw new Error('Ошибка при обработке файлов сервером.');

                const data = await response.json();

                // ВАЖНО: Сохраняем данные глобально! Код напарницы будет читать их отсюда.
                window.loadedQuestions = data.questions;

                // Отрисовываем таблицу
                renderTable(data.questions);

                statusDiv.classList.remove('alert-warning');
                statusDiv.classList.add('alert-success');
                statusDiv.textContent = `Успешно! Студентов учтено: ${data.totalStudentsAnalyzed}`;
            } catch (error) {
                statusDiv.classList.remove('alert-warning');
                statusDiv.classList.add('alert-danger');
                statusDiv.textContent = error.message;
            } finally {
                btn.disabled = false;
            }
        });
    }
});

// Функция отрисовки таблицы Светофора
function renderTable(questions) {
    const tbody = document.getElementById('resultsBody');
    tbody.innerHTML = '';

    questions.forEach(q => {
        const tr = document.createElement('tr');

        let badgeClass = '';
        let zoneName = '';
        let rowClass = '';

        // Маппинг цвета зоны (0 - Green, 1 - Yellow, 2 - Red)
        if (q.zone === 2) {
            badgeClass = 'bg-danger';
            zoneName = 'Красная (6+)';
            rowClass = 'table-danger';
        } else if (q.zone === 1) {
            badgeClass = 'bg-warning text-dark';
            zoneName = 'Желтая (4-5)';
            rowClass = 'table-warning';
        } else {
            badgeClass = 'bg-success';
            zoneName = 'Зеленая (1-3)';
            rowClass = 'table-success';
        }

        tr.className = rowClass;

        // Обрезаем слишком длинные вопросы для красоты таблицы
        const shortText = q.questionText && q.questionText.length > 80
            ? q.questionText.substring(0, 80) + '...'
            : q.questionText;

        tr.innerHTML = `
            <td class="fw-bold">${q.questionId}</td>
            <td title="${q.questionText}">${shortText}</td>
            <td class="text-success fw-bold">${q.correctCount}</td>
            <td class="text-danger fw-bold">${q.errorsCount}</td>
            <td><span class="badge ${badgeClass}">${zoneName}</span></td>
        `;
        tbody.appendChild(tr);
    });
}