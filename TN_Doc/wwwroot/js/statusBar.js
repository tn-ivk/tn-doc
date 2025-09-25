/**
 * Status Bar Manager для TN_Doc
 * Управляет строкой состояния с индикаторами подключений
 */
class StatusBarManager {
    constructor() {
        this.indicators = new Map();
        this.signalRConnection = null;
        this.updateInterval = 30000; // 30 секунд
        this.isInitialized = false;
        this.deviceConfig = null;
        this.lastUpdateTime = null;
        this.retryCount = 0;
        this.maxRetries = 3;

        // Привязка методов к контексту
        this.refreshStatus = this.refreshStatus.bind(this);
        this.handleRefreshClick = this.handleRefreshClick.bind(this);

        this.init();
    }

    /**
     * Инициализация менеджера строки состояния
     */
    async init() {
        try {
            console.log('[StatusBar] Инициализация строки состояния...');

            await this.loadDeviceConfiguration();
            this.createDeviceIndicators();
            this.createOpcIndicators();
            this.initializeSignalR();
            this.setupEventListeners();
            this.startPeriodicUpdates();

            this.isInitialized = true;
            console.log('[StatusBar] Строка состояния инициализирована');
        } catch (error) {
            console.error('[StatusBar] Ошибка инициализации:', error);
        }
    }

    /**
     * Загрузка конфигурации устройств
     */
    async loadDeviceConfiguration() {
        try {
            const response = await fetch('/api/status/config');
            if (response.ok) {
                this.deviceConfig = await response.json();
                console.log('[StatusBar] Конфигурация загружена:', this.deviceConfig);
            } else {
                console.warn('[StatusBar] Не удалось загрузить конфигурацию устройств');
                // Fallback конфигурация
                this.deviceConfig = {
                    devices: [],
                    opcServers: [],
                    elis: { use: false }
                };
            }
        } catch (error) {
            console.error('[StatusBar] Ошибка загрузки конфигурации:', error);
            this.deviceConfig = {
                devices: [],
                opcServers: [],
                elis: { use: false }
            };
        }
    }

    /**
     * Создание индикаторов устройств
     */
    createDeviceIndicators() {
        const container = document.getElementById('device-indicators');
        if (!container || !this.deviceConfig.devices) return;

        container.innerHTML = '';

        this.deviceConfig.devices.forEach(device => {
            const indicator = this.createIndicator({
                id: `device-${device.idDevice}`,
                text: device.name || `ИВК-${device.idDevice}`,
                tooltip: `Подключение к БД устройства ${device.name}`,
                type: 'device',
                deviceId: device.idDevice
            });

            container.appendChild(indicator);
            this.indicators.set(`device-${device.idDevice}`, {
                element: indicator,
                type: 'device',
                deviceId: device.idDevice,
                lastStatus: 'unknown'
            });
        });
    }

    /**
     * Создание индикаторов OPC серверов
     */
    createOpcIndicators() {
        const container = document.getElementById('opc-indicators');
        if (!container || !this.deviceConfig.opcServers) return;

        container.innerHTML = '';

        this.deviceConfig.opcServers.forEach(opcServer => {
            const indicator = this.createIndicator({
                id: `opc-${opcServer.id}`,
                text: `OPC ${opcServer.type}`,
                tooltip: `${opcServer.type} сервер: ${opcServer.endpoint || opcServer.progId}`,
                type: 'opc',
                opcType: opcServer.type
            });

            container.appendChild(indicator);
            this.indicators.set(`opc-${opcServer.id}`, {
                element: indicator,
                type: 'opc',
                opcId: opcServer.id,
                opcType: opcServer.type,
                lastStatus: 'unknown'
            });
        });
    }

    /**
     * Создание HTML элемента индикатора
     */
    createIndicator(config) {
        const span = document.createElement('span');
        span.id = config.id;
        span.className = `status-indicator status-${config.type} status-unknown`;
        span.setAttribute('data-service', config.type);

        const textSpan = document.createElement('span');
        textSpan.className = 'status-text';
        textSpan.textContent = config.text;

        const tooltip = document.createElement('div');
        tooltip.className = 'status-tooltip';
        tooltip.textContent = config.tooltip;

        span.appendChild(textSpan);
        span.appendChild(tooltip);

        // Добавляем обработчик клика для обновления конкретного индикатора
        span.addEventListener('click', () => {
            if (config.type === 'device') {
                this.refreshDeviceStatus(config.deviceId);
            } else if (config.type === 'opc') {
                this.refreshOpcStatus(config.opcId);
            }
        });

        return span;
    }

    /**
     * Инициализация SignalR подключения
     */
    async initializeSignalR() {
        try {
            const signalRUrl = 'http://localhost:5010/SignalRApp';
            console.log('[StatusBar] Инициализация SignalR:', signalRUrl);

            this.signalRConnection = new signalR.HubConnectionBuilder()
                .withUrl(signalRUrl)
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .build();

            // Обработчики событий SignalR
            this.signalRConnection.onreconnecting(() => {
                console.log('[StatusBar] SignalR переподключается...');
                this.updateSignalRStatus('checking');
            });

            this.signalRConnection.onreconnected(() => {
                console.log('[StatusBar] SignalR переподключен');
                this.updateSignalRStatus('connected');
            });

            this.signalRConnection.onclose(() => {
                console.log('[StatusBar] SignalR отключен');
                this.updateSignalRStatus('disconnected');
            });

            // Подписка на обновления статуса
            this.signalRConnection.on('StatusUpdate', (statusData) => {
                this.handleStatusUpdate(statusData);
            });

            // Попытка подключения
            await this.connectSignalR();

        } catch (error) {
            console.error('[StatusBar] Ошибка инициализации SignalR:', error);
            this.updateSignalRStatus('disconnected');
        }
    }

    /**
     * Подключение к SignalR
     */
    async connectSignalR() {
        try {
            this.updateSignalRStatus('checking');
            await this.signalRConnection.start();
            console.log('[StatusBar] SignalR подключен');
            this.updateSignalRStatus('connected');

            // Присоединяемся к группе обновлений статуса
            await this.signalRConnection.invoke('JoinStatusGroup');
        } catch (error) {
            console.error('[StatusBar] Ошибка подключения SignalR:', error);
            this.updateSignalRStatus('disconnected');
        }
    }

    /**
     * Настройка обработчиков событий
     */
    setupEventListeners() {
        const refreshBtn = document.getElementById('refresh-status');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', this.handleRefreshClick);
        }

        // Показать/скрыть ELIS индикатор
        if (this.deviceConfig.elis && this.deviceConfig.elis.use) {
            const elisIndicator = document.getElementById('elis-status');
            if (elisIndicator) {
                elisIndicator.style.display = 'inline-flex';
            }
        }
    }

    /**
     * Запуск периодических обновлений
     */
    startPeriodicUpdates() {
        // Обновляем статусы каждые 30 секунд
        setInterval(() => {
            this.refreshStatus();
        }, this.updateInterval);

        // Первое обновление через 2 секунды после инициализации
        setTimeout(() => {
            this.refreshStatus();
        }, 2000);
    }


    /**
     * Обновление всех статусов
     */
    async refreshStatus() {
        if (!this.isInitialized) return;

        try {
            console.log('[StatusBar] Обновление статусов...');

            // Параллельное обновление всех типов статусов
            const promises = [
                this.refreshDeviceStatuses(),
                this.refreshOpcStatuses(),
                this.refreshServiceStatuses()
            ];

            await Promise.allSettled(promises);

            this.lastUpdateTime = new Date();
            this.retryCount = 0;

            console.log('[StatusBar] Статусы обновлены');
        } catch (error) {
            console.error('[StatusBar] Ошибка обновления статусов:', error);
            this.retryCount++;
        }
    }

    /**
     * Обновление статусов устройств
     */
    async refreshDeviceStatuses() {
        try {
            const response = await fetch('/api/status/devices');
            if (response.ok) {
                const statuses = await response.json();
                statuses.forEach(status => {
                    this.updateDeviceStatus(status.deviceId, status.isConnected, status.responseTime, status.connectionInfo);
                });
            }
        } catch (error) {
            console.error('[StatusBar] Ошибка получения статусов устройств:', error);
        }
    }

    /**
     * Обновление статусов OPC серверов
     */
    async refreshOpcStatuses() {
        try {
            const response = await fetch('/api/status/opc');
            if (response.ok) {
                const statuses = await response.json();
                statuses.forEach(status => {
                    this.updateOpcStatus(status.opcId, status.isConnected, status.responseTime, status.connectionInfo);
                });
            }
        } catch (error) {
            console.error('[StatusBar] Ошибка получения статусов OPC:', error);
        }
    }

    /**
     * Обновление статусов сервисов
     */
    async refreshServiceStatuses() {
        try {
            const response = await fetch('/api/status/services');
            if (response.ok) {
                const statuses = await response.json();

                if (statuses.elis && this.deviceConfig.elis.use) {
                    this.updateElisStatus(statuses.elis.isAvailable, statuses.elis.responseTime, statuses.elis.status);
                }
            }
        } catch (error) {
            console.error('[StatusBar] Ошибка получения статусов сервисов:', error);
        }
    }

    /**
     * Обновление статуса конкретного устройства
     */
    async refreshDeviceStatus(deviceId) {
        try {
            const response = await fetch(`/api/status/device/${deviceId}`);
            if (response.ok) {
                const status = await response.json();
                this.updateDeviceStatus(status.deviceId, status.isConnected, status.responseTime, status.connectionInfo);
            }
        } catch (error) {
            console.error(`[StatusBar] Ошибка получения статуса устройства ${deviceId}:`, error);
        }
    }

    /**
     * Обновление статуса конкретного OPC сервера
     */
    async refreshOpcStatus(opcId) {
        try {
            const response = await fetch(`/api/status/opc/${opcId}`);
            if (response.ok) {
                const status = await response.json();
                this.updateOpcStatus(status.opcId, status.isConnected, status.responseTime, status.connectionInfo);
            }
        } catch (error) {
            console.error(`[StatusBar] Ошибка получения статуса OPC ${opcId}:`, error);
        }
    }

    /**
     * Обновление статуса устройства в UI
     */
    updateDeviceStatus(deviceId, isConnected, responseTime, connectionInfo) {
        const indicator = this.indicators.get(`device-${deviceId}`);
        if (!indicator) return;

        const newStatus = isConnected ? 'connected' : 'disconnected';
        this.updateIndicatorStatus(indicator.element, newStatus);

        const tooltip = indicator.element.querySelector('.status-tooltip');
        if (tooltip) {
            tooltip.textContent = connectionInfo || `Устройство ${deviceId}: ${isConnected ? 'подключено' : 'отключено'}`;
        }

        indicator.lastStatus = newStatus;
    }

    /**
     * Обновление статуса OPC сервера в UI
     */
    updateOpcStatus(opcId, isConnected, responseTime, connectionInfo) {
        const indicator = this.indicators.get(`opc-${opcId}`);
        if (!indicator) return;

        const newStatus = isConnected ? 'connected' : 'disconnected';
        this.updateIndicatorStatus(indicator.element, newStatus);

        const tooltip = indicator.element.querySelector('.status-tooltip');
        if (tooltip) {
            tooltip.textContent = connectionInfo || `OPC ${opcId}: ${isConnected ? 'подключено' : 'отключено'}`;
        }

        indicator.lastStatus = newStatus;
    }

    /**
     * Обновление статуса SignalR в UI
     */
    updateSignalRStatus(status) {
        const element = document.getElementById('signalr-status');
        if (!element) return;

        this.updateIndicatorStatus(element, status);

        const tooltip = element.querySelector('.status-tooltip');
        if (tooltip) {
            const statusText = {
                'connected': 'Подключен к SignalR Hub',
                'disconnected': 'Отключен от SignalR Hub',
                'checking': 'Подключение к SignalR Hub...'
            };
            tooltip.textContent = statusText[status] || 'Неизвестный статус SignalR';
        }
    }

    /**
     * Обновление статуса ELIS в UI
     */
    updateElisStatus(isAvailable, responseTime, statusText) {
        const element = document.getElementById('elis-status');
        if (!element) return;

        const status = isAvailable ? 'connected' : 'disconnected';
        this.updateIndicatorStatus(element, status);

        const tooltip = element.querySelector('.status-tooltip');
        if (tooltip) {
            tooltip.textContent = statusText || `ELIS: ${isAvailable ? 'доступен' : 'недоступен'}`;
        }
    }

    /**
     * Универсальное обновление статуса индикатора
     */
    updateIndicatorStatus(element, status) {
        if (!element) return;

        // Удаляем все классы статуса
        element.classList.remove('status-connected', 'status-disconnected', 'status-checking', 'status-disabled', 'status-unknown');

        // Добавляем новый класс статуса
        element.classList.add(`status-${status}`);
    }

    /**
     * Обработка обновлений статуса от SignalR
     */
    handleStatusUpdate(statusData) {
        console.log('[StatusBar] Получено обновление статуса:', statusData);

        if (statusData.type === 'device') {
            this.updateDeviceStatus(statusData.deviceId, statusData.isConnected, statusData.responseTime, statusData.connectionInfo);
        } else if (statusData.type === 'opc') {
            this.updateOpcStatus(statusData.opcId, statusData.isConnected, statusData.responseTime, statusData.connectionInfo);
        } else if (statusData.type === 'elis') {
            this.updateElisStatus(statusData.isAvailable, statusData.responseTime, statusData.status);
        }
    }

    /**
     * Обработка клика по кнопке обновления
     */
    async handleRefreshClick() {
        const refreshBtn = document.getElementById('refresh-status');
        if (refreshBtn) {
            refreshBtn.disabled = true;
            refreshBtn.innerHTML = '<div class="status-loading"></div>';
        }

        try {
            await this.refreshStatus();
        } finally {
            if (refreshBtn) {
                refreshBtn.disabled = false;
                refreshBtn.innerHTML = '↻';
            }
        }
    }

    /**
     * Получение текущего состояния строки состояния
     */
    getStatus() {
        const result = {
            initialized: this.isInitialized,
            lastUpdate: this.lastUpdateTime,
            signalRConnected: this.signalRConnection && this.signalRConnection.state === signalR.HubConnectionState.Connected,
            indicators: {}
        };

        this.indicators.forEach((indicator, key) => {
            result.indicators[key] = {
                type: indicator.type,
                status: indicator.lastStatus
            };
        });

        return result;
    }

    /**
     * Уничтожение менеджера строки состояния
     */
    destroy() {
        // Отключаем SignalR
        if (this.signalRConnection) {
            this.signalRConnection.stop();
        }

        // Очищаем индикаторы
        this.indicators.clear();

        // Удаляем обработчики событий
        const refreshBtn = document.getElementById('refresh-status');
        if (refreshBtn) {
            refreshBtn.removeEventListener('click', this.handleRefreshClick);
        }

        this.isInitialized = false;
        console.log('[StatusBar] Менеджер строки состояния уничтожен');
    }
}