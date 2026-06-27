Clear-Host
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "     ДОБРО ПОЖАЛОВАТЬ В СИСТЕМУ АНАЛИТИКИ КУРСОВ" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# --- 0. АВТОЗАПУСК DOCKER DESKTOP ---
Write-Host "[0/2] Проверка статуса Docker Desktop..." -ForegroundColor Yellow

docker info > $null 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker Desktop выключен. Попытка автоматического запуска..." -ForegroundColor DarkYellow
    $dockerPath = "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    
    if (Test-Path $dockerPath) {
        Start-Process $dockerPath
        Write-Host "Ожидание запуска службы Docker (это может занять до 30 секунд)..." -ForegroundColor Gray
        while ($true) {
            Start-Sleep -Seconds 3
            docker info > $null 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✔ Docker Desktop успешно запущен и готов!" -ForegroundColor Green
                break
            }
            Write-Progress -Activity "Запуск Docker Desktop" -Status "Ожидание ответа от демона..."
        }
    } else {
        Write-Host "❌ Ошибка: Docker Desktop не найден по стандартному пути!" -ForegroundColor Red
        Write-Host "Пожалуйста, запустите Docker Desktop вручную и повторите попытку." -ForegroundColor Red
        Read-Host "Нажмите Enter для выхода..."
        Exit
    }
} else {
    Write-Host "✔ Docker Desktop уже запущен." -ForegroundColor Green
}
Write-Host ""

# --- 1. ПРЕДУПРЕЖДЕНИЕ И ВЫБОР ИНФРАСТРУКТУРЫ ИИ ---
Write-Host "[1/2] Настройка локальной нейросети Ollama" -ForegroundColor Yellow
Write-Host "ВНИМАНИЕ: Для работы локального ИИ требуется скачать Docker-образ Ollama и модель Llama 3." -ForegroundColor DarkCyan
Write-Host "Суммарный объем загрузки составит около 7 ГБ. Убедитесь, что у вас стабильный интернет." -ForegroundColor DarkCyan
Write-Host ""
Write-Host "Установите режим запуска:" -ForegroundColor White
Write-Host "[Y] - Скачать и запустить Ollama внутри Docker (все 'из коробки', ~7 ГБ скачивания)" -ForegroundColor Gray
Write-Host "[N] - Пропустить. Выбирайте, если Ollama УЖЕ запущена в вашей Windows, или если вы используете облачное API" -ForegroundColor Gray
Write-Host ""

$choice = Read-Host "Ваш выбор (Y/N)"
$runOllamaInDocker = $false

if ($choice -eq "Y" -or $choice -eq "y" -or $choice -eq "д" -or $choice -eq "Д") {
    $runOllamaInDocker = $true
    Write-Host "Принято. В оркестратор включен полный пакет (Веб-сайт + Ollama)." -ForegroundColor Green
} else {
    Write-Host "Принято. Будет запущен ТОЛЬКО контейнер веб-приложения." -ForegroundColor Magenta
}
Write-Host ""

# --- 2. ЗАПУСК ОРКЕСТРАТОРА DOCKER COMPOSE ---
Write-Host "[2/2] Запуск Docker-оркестратора..." -ForegroundColor Yellow

if ($runOllamaInDocker) {
    # Запускаем оба контейнера (и веб, и локальный ИИ)
    docker-compose up -d --build
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✔ Оркестратор успешно запущен!" -ForegroundColor Green
        Write-Host "Начинается скачивание модели Llama 3 внутрь контейнера..." -ForegroundColor Cyan
        docker exec -it aianalyzer-ollama-server-1 ollama run llama3
        Write-Host "✔ Модель успешно загружена!" -ForegroundColor Green
    }
} else {
    # Запускаем ТОЛЬКО веб-приложение, чтобы сэкономить 7 ГБ
    docker-compose up -d --build analyzer-web
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✔ Контейнер веб-приложения успешно запущен!" -ForegroundColor Green
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "Ошибка при сборке контейнеров!" -ForegroundColor Red
    Read-Host "Нажмите Enter для выхода..."
    Exit
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " Сборка завершена! Сайт доступен по адресу: http://localhost:8080" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Read-Host "Нажмите Enter для завершения..."