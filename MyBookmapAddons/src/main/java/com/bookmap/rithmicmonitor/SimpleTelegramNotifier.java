package com.bookmap.rithmicmonitor;

import velox.api.layer1.Layer1ApiAdminAdapter;
import velox.api.layer1.Layer1ApiFinishable;
import velox.api.layer1.Layer1ApiProvider;
import velox.api.layer1.annotations.Layer1ApiVersion;
import velox.api.layer1.annotations.Layer1ApiVersionValue;
import velox.api.layer1.annotations.Layer1Attachable;
import velox.api.layer1.annotations.Layer1StrategyName;
import velox.api.layer1.common.ListenableHelper;

import javax.swing.*;
import java.awt.*;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;
import java.io.File;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.util.Properties;

@Layer1Attachable
@Layer1StrategyName("Simple Telegram Notifier")
@Layer1ApiVersion(Layer1ApiVersionValue.VERSION2)
public class SimpleTelegramNotifier implements
        Layer1ApiAdminAdapter,
        Layer1ApiFinishable,
        velox.api.layer1.Layer1CustomPanelsGetter,
        velox.api.layer1.Layer1ApiDataListener {

    private final Layer1ApiProvider provider;
    private final ScheduledExecutorService scheduler = Executors.newScheduledThreadPool(1);
    private final ConcurrentHashMap<String, Long> lastDataTime = new ConcurrentHashMap<>();
    
    private String botToken = "";
    private String chatId = "";
    private int timeoutSeconds = 30;
    private int periodicSeconds = 0;
    private boolean isMonitoring = false;
    private boolean isInTimeoutState = false;
    private boolean isPeriodicScheduled = false;
    private long timeoutStartTime = 0;
    private java.util.concurrent.ScheduledFuture<?> timeoutTask;
    private java.util.concurrent.ScheduledFuture<?> periodicTask;
    
    private JTextField botTokenField;
    private JTextField chatIdField;
    private JTextField timeoutField;
    private JTextField periodicField;
    private JLabel statusLabel;
    private final File configFile;
    
    public SimpleTelegramNotifier(Layer1ApiProvider provider) {
        this.provider = provider;
        this.configFile = new File(System.getProperty("user.home"), "SimpleTelegramNotifier.properties");
        ListenableHelper.addListeners(provider, this);
        loadConfig(); // Load saved configuration on startup
    }
    
    @Override
    public void onUserMessage(Object data) {
        if (data instanceof velox.api.layer1.messages.UserMessageLayersChainCreatedTargeted) {
            velox.api.layer1.messages.UserMessageLayersChainCreatedTargeted message = 
                (velox.api.layer1.messages.UserMessageLayersChainCreatedTargeted) data;
            if (message.targetClass == getClass()) {
                System.out.println("=== Simple Telegram Notifier Started ===");
                System.out.println("To set Telegram config: setTelegramConfig(botToken, chatId)");
                System.out.println("To test: testTelegram()");
                System.out.println("To send message: sendMessage(\"your message\")");
                System.out.println("To start monitoring: startMonitoring()");
                System.out.println("To stop monitoring: stopMonitoring()");
                System.out.println("=========================================");
            }
        }
    }
    
    @Override
    public void onDepth(String alias, boolean isBid, int price, int size) {
        lastDataTime.put(alias, System.currentTimeMillis());
        // Reset timeout state when new data arrives
        if (isInTimeoutState) {
            isInTimeoutState = false;
            isPeriodicScheduled = false;
            timeoutStartTime = 0;
            
            // Cancel periodic task if it's running
            if (periodicTask != null && !periodicTask.isCancelled()) {
                periodicTask.cancel(false);
                periodicTask = null;
            }
            
            System.out.println("✅ Data received - timeout state reset");
        }
    }
    
    @Override
    public void onMarketMode(String alias, velox.api.layer1.data.MarketMode mode) {
        lastDataTime.put(alias, System.currentTimeMillis());
    }
    
    @Override
    public void onTrade(String alias, double price, int size, velox.api.layer1.data.TradeInfo tradeInfo) {
        lastDataTime.put(alias, System.currentTimeMillis());
    }
    
    public void startMonitoring() {
        if (!isMonitoring) {
            isMonitoring = true;
            isInTimeoutState = false;
            isPeriodicScheduled = false;
            timeoutStartTime = 0;
            timeoutTask = scheduler.scheduleAtFixedRate(this::checkDataTimeout, 5, 5, TimeUnit.SECONDS);
            System.out.println("✅ Data monitoring started - timeout: " + timeoutSeconds + " seconds, periodic: " + periodicSeconds + " seconds");
            updateStatus();
        }
    }
    
    public void stopMonitoring() {
        if (isMonitoring) {
            isMonitoring = false;
            isInTimeoutState = false;
            isPeriodicScheduled = false;
            timeoutStartTime = 0;
            
            // Cancel scheduled tasks
            if (timeoutTask != null && !timeoutTask.isCancelled()) {
                timeoutTask.cancel(false);
                timeoutTask = null;
            }
            if (periodicTask != null && !periodicTask.isCancelled()) {
                periodicTask.cancel(false);
                periodicTask = null;
            }
            
            System.out.println("⏹️ Data monitoring stopped");
            updateStatus();
        }
    }
    
    private void updateStatus() {
        if (statusLabel != null) {
            SwingUtilities.invokeLater(() -> {
                if (isMonitoring) {
                    statusLabel.setText("Status: Monitoring Active");
                    statusLabel.setForeground(Color.GREEN);
                } else {
                    statusLabel.setText("Status: Not Monitoring");
                    statusLabel.setForeground(Color.RED);
                }
            });
        }
    }
    
    private void checkDataTimeout() {
        if (!isMonitoring) return;
        
        long currentTime = System.currentTimeMillis();
        long timeoutMs = timeoutSeconds * 1000L;
        
        boolean hasRecentData = lastDataTime.values().stream()
                .anyMatch(lastTime -> (currentTime - lastTime) < timeoutMs);
        
        if (!hasRecentData && !lastDataTime.isEmpty()) {
            if (!isInTimeoutState) {
                // First timeout alert
                timeoutStartTime = System.currentTimeMillis();
                System.out.println("🔴 First timeout triggered after " + timeoutSeconds + " seconds");
                sendMessage("⚠️ No connection after " + timeoutSeconds + " seconds");
                isInTimeoutState = true;
                
                // Start periodic alerts if configured
                if (periodicSeconds > 0 && !isPeriodicScheduled) {
                    isPeriodicScheduled = true;
                    System.out.println("⏰ Starting periodic alerts every " + periodicSeconds + " seconds");
                    // Start periodic alerts after the first periodic interval to avoid duplicate messages
                    periodicTask = scheduler.scheduleAtFixedRate(this::sendPeriodicTimeoutAlert, periodicSeconds, periodicSeconds, TimeUnit.SECONDS);
                }
            }
        }
    }
    
    private void sendPeriodicTimeoutAlert() {
        if (!isMonitoring || !isInTimeoutState) {
            return;
        }
        
        long currentTime = System.currentTimeMillis();
        long timeoutMs = timeoutSeconds * 1000L;
        boolean hasRecentData = lastDataTime.values().stream()
                .anyMatch(lastTime -> (currentTime - lastTime) < timeoutMs);
        
        if (!hasRecentData && !lastDataTime.isEmpty()) {
            // Calculate how many periodic intervals have passed since timeout started
            long timeSinceTimeout = currentTime - timeoutStartTime;
            long periodicIntervalsPassed = timeSinceTimeout / (periodicSeconds * 1000L);
            long totalSeconds = timeoutSeconds + (periodicIntervalsPassed * periodicSeconds);
            
            System.out.println("📡 Periodic alert: " + totalSeconds + " seconds total");
            sendMessage("⚠️ No connection after " + totalSeconds + " seconds");
        } else {
            // Data has returned, stop periodic alerts
            System.out.println("✅ Data returned, stopping periodic alerts");
            isInTimeoutState = false;
            isPeriodicScheduled = false;
            timeoutStartTime = 0;
            
            // Cancel periodic task
            if (periodicTask != null && !periodicTask.isCancelled()) {
                periodicTask.cancel(false);
                periodicTask = null;
            }
        }
    }
    
    @Override
    public velox.gui.StrategyPanel[] getCustomGuiFor(String indicatorName, String indicatorFullName) {
        JPanel panel = new JPanel(new GridBagLayout());
        GridBagConstraints gbc = new GridBagConstraints();
        gbc.insets = new Insets(5, 5, 5, 5);
        gbc.fill = GridBagConstraints.HORIZONTAL;
        
        // Bot Token
        gbc.gridx = 0; gbc.gridy = 0;
        panel.add(new JLabel("Bot Token:"), gbc);
        
        gbc.gridx = 1; gbc.gridy = 0;
        gbc.weightx = 1.0;
        botTokenField = new JTextField(15);
        botTokenField.setText(botToken); // Set saved value
        panel.add(botTokenField, gbc);
        
        // Chat ID
        gbc.gridx = 0; gbc.gridy = 1;
        gbc.weightx = 0.0;
        panel.add(new JLabel("Chat ID:"), gbc);
        
        gbc.gridx = 1; gbc.gridy = 1;
        gbc.weightx = 1.0;
        chatIdField = new JTextField(15);
        chatIdField.setText(chatId); // Set saved value
        panel.add(chatIdField, gbc);
        
        // Timeout
        gbc.gridx = 0; gbc.gridy = 2;
        gbc.weightx = 0.0;
        panel.add(new JLabel("Timeout:"), gbc);
        
        gbc.gridx = 1; gbc.gridy = 2;
        gbc.weightx = 1.0;
        timeoutField = new JTextField(String.valueOf(timeoutSeconds), 8);
        panel.add(timeoutField, gbc);
        
        // Periodic
        gbc.gridx = 0; gbc.gridy = 3;
        gbc.weightx = 0.0;
        panel.add(new JLabel("Periodic:"), gbc);
        
        gbc.gridx = 1; gbc.gridy = 3;
        gbc.weightx = 1.0;
        periodicField = new JTextField(String.valueOf(periodicSeconds), 8);
        panel.add(periodicField, gbc);
        
        // Status Display
        gbc.gridx = 0; gbc.gridy = 4;
        gbc.gridwidth = 2;
        gbc.weightx = 0.0;
        statusLabel = new JLabel("Status: Not Monitoring", SwingConstants.CENTER);
        statusLabel.setForeground(Color.RED);
        statusLabel.setFont(new Font("Arial", Font.BOLD, 12));
        panel.add(statusLabel, gbc);
        
        // Update status display with current state
        updateStatus();
        
        // Buttons in Column
        JPanel buttonPanel = new JPanel(new GridLayout(0, 1, 5, 5));
        buttonPanel.setBorder(BorderFactory.createEmptyBorder(10, 0, 10, 0));
        
        JButton testButton = new JButton("Test");
        testButton.addActionListener(e -> {
            saveConfig();
            testTelegram();
        });
        buttonPanel.add(testButton);
        
        JButton sendButton = new JButton("Send Message");
        sendButton.addActionListener(e -> {
            saveConfig();
            String message = JOptionPane.showInputDialog(panel, "Enter message to send:");
            if (message != null && !message.trim().isEmpty()) {
                sendMessage(message.trim());
            }
        });
        buttonPanel.add(sendButton);
        
        JButton startButton = new JButton("Start Monitor");
        startButton.addActionListener(e -> {
            saveConfig();
            startMonitoring();
        });
        buttonPanel.add(startButton);
        
        JButton stopButton = new JButton("Stop Monitor");
        stopButton.addActionListener(e -> {
            saveConfig();
            stopMonitoring();
        });
        buttonPanel.add(stopButton);
        
        gbc.gridx = 0; gbc.gridy = 5;
        gbc.gridwidth = 2;
        gbc.weightx = 0.0;
        panel.add(buttonPanel, gbc);
        
        velox.gui.StrategyPanel strategyPanel = new velox.gui.StrategyPanel("Telegram Settings", true);
        strategyPanel.add(panel);
        return new velox.gui.StrategyPanel[] { strategyPanel };
    }
    
    public void setTelegramConfig(String botToken, String chatId) {
        this.botToken = botToken;
        this.chatId = chatId;
        if (botTokenField != null) {
            botTokenField.setText(botToken);
        }
        if (chatIdField != null) {
            chatIdField.setText(chatId);
        }
        System.out.println("Telegram config set - Bot: " + botToken.substring(0, Math.min(10, botToken.length())) + "...");
        System.out.println("Chat ID: " + chatId);
    }
    
    public void setTimeout(int seconds) {
        this.timeoutSeconds = seconds;
        if (timeoutField != null) {
            timeoutField.setText(String.valueOf(seconds));
        }
        System.out.println("Timeout set to: " + seconds + " seconds");
    }
    
    private void saveConfig() {
        botToken = botTokenField.getText().trim();
        chatId = chatIdField.getText().trim();
        try {
            timeoutSeconds = Integer.parseInt(timeoutField.getText().trim());
        } catch (NumberFormatException ex) {
            timeoutSeconds = 30;
            timeoutField.setText("30");
        }
        try {
            periodicSeconds = Integer.parseInt(periodicField.getText().trim());
        } catch (NumberFormatException ex) {
            periodicSeconds = 0;
            periodicField.setText("0");
        }
        
        // Save to file
        saveConfigToFile();
        
        System.out.println("Config saved - Bot: " + botToken.substring(0, Math.min(10, botToken.length())) + "...");
        System.out.println("Chat ID: " + chatId);
        System.out.println("Timeout: " + timeoutSeconds + " seconds");
        System.out.println("Periodic: " + periodicSeconds + " seconds");
    }
    
    private void saveConfigToFile() {
        try {
            Properties props = new Properties();
            props.setProperty("botToken", botToken);
            props.setProperty("chatId", chatId);
            props.setProperty("timeoutSeconds", String.valueOf(timeoutSeconds));
            props.setProperty("periodicSeconds", String.valueOf(periodicSeconds));
            
            try (FileWriter writer = new FileWriter(configFile)) {
                props.store(writer, "Simple Telegram Notifier Configuration");
            }
            System.out.println("✅ Configuration saved to: " + configFile.getAbsolutePath());
        } catch (IOException e) {
            System.err.println("❌ Error saving configuration: " + e.getMessage());
        }
    }
    
    private void loadConfig() {
        if (!configFile.exists()) {
            System.out.println("No saved configuration found");
            return;
        }
        
        try {
            Properties props = new Properties();
            try (FileReader reader = new FileReader(configFile)) {
                props.load(reader);
            }
            
            botToken = props.getProperty("botToken", "");
            chatId = props.getProperty("chatId", "");
            timeoutSeconds = Integer.parseInt(props.getProperty("timeoutSeconds", "30"));
            periodicSeconds = Integer.parseInt(props.getProperty("periodicSeconds", "0"));
            
            // Update UI fields if they exist
            if (botTokenField != null) botTokenField.setText(botToken);
            if (chatIdField != null) chatIdField.setText(chatId);
            if (timeoutField != null) timeoutField.setText(String.valueOf(timeoutSeconds));
            if (periodicField != null) periodicField.setText(String.valueOf(periodicSeconds));
            
            System.out.println("✅ Configuration loaded from: " + configFile.getAbsolutePath());
            System.out.println("Bot: " + botToken.substring(0, Math.min(10, botToken.length())) + "...");
            System.out.println("Chat ID: " + chatId);
            System.out.println("Timeout: " + timeoutSeconds + " seconds");
            System.out.println("Periodic: " + periodicSeconds + " seconds");
            
            // Update status display if UI is already created
            updateStatus();
        } catch (IOException | NumberFormatException e) {
            System.err.println("❌ Error loading configuration: " + e.getMessage());
        }
    }
    
    public void testTelegram() {
        sendMessage("🧪 Test message from Simple Telegram Notifier");
    }
    
    public void sendMessage(String message) {
        if (botToken.isEmpty() || chatId.isEmpty()) {
            System.out.println("Telegram not configured. Use setTelegramConfig(botToken, chatId) first.");
            return;
        }
        
        try {
            String encodedMessage = URLEncoder.encode(message, StandardCharsets.UTF_8.toString());
            String urlString = String.format("https://api.telegram.org/bot%s/sendMessage?chat_id=%s&text=%s", 
                                           botToken, chatId, encodedMessage);
            
            URL url = new URL(urlString);
            HttpURLConnection connection = (HttpURLConnection) url.openConnection();
            connection.setRequestMethod("GET");
            connection.setConnectTimeout(5000);
            connection.setReadTimeout(5000);
            
            int responseCode = connection.getResponseCode();
            
            if (responseCode == 200) {
                System.out.println("✅ Telegram message sent: " + message);
            } else {
                System.out.println("❌ Failed to send Telegram message. Status: " + responseCode);
            }
            
            connection.disconnect();
            
        } catch (Exception e) {
            System.err.println("❌ Error sending Telegram message: " + e.getMessage());
        }
    }
    
    @Override
    public void finish() {
        stopMonitoring();
        if (scheduler != null && !scheduler.isShutdown()) {
            scheduler.shutdown();
            try {
                if (!scheduler.awaitTermination(5, TimeUnit.SECONDS)) {
                    scheduler.shutdownNow();
                }
            } catch (InterruptedException e) {
                scheduler.shutdownNow();
                Thread.currentThread().interrupt();
            }
        }
        System.out.println("Simple Telegram Notifier stopped");
        ListenableHelper.removeListeners(provider, this);
    }
} 