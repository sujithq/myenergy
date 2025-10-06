// Chart.js helper functions for Blazor integration

let chartInstances = {};

window.createChart = (canvasId, config) => {
    try {
        // Destroy existing chart if it exists
        if (chartInstances[canvasId]) {
            chartInstances[canvasId].destroy();
            delete chartInstances[canvasId];
        }

        // Get the canvas element
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error(`Canvas element with id '${canvasId}' not found`);
            return;
        }

        // Create new chart
        chartInstances[canvasId] = new Chart(ctx, config);
        
        console.log(`Chart created successfully for canvas: ${canvasId}`);
    } catch (error) {
        console.error('Error creating chart:', error);
    }
};

window.destroyChart = (canvasId) => {
    try {
        if (chartInstances[canvasId]) {
            chartInstances[canvasId].destroy();
            delete chartInstances[canvasId];
            console.log(`Chart destroyed for canvas: ${canvasId}`);
        }
    } catch (error) {
        console.error('Error destroying chart:', error);
    }
};

window.updateChartData = (canvasId, newData) => {
    try {
        if (chartInstances[canvasId]) {
            const chart = chartInstances[canvasId];
            chart.data = newData;
            chart.update();
            console.log(`Chart data updated for canvas: ${canvasId}`);
        }
    } catch (error) {
        console.error('Error updating chart data:', error);
    }
};

// Clean up charts when page is unloaded
window.addEventListener('beforeunload', () => {
    Object.keys(chartInstances).forEach(canvasId => {
        if (chartInstances[canvasId]) {
            chartInstances[canvasId].destroy();
        }
    });
    chartInstances = {};
});

// Chart.js default configuration
Chart.defaults.font.family = "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif";
Chart.defaults.color = '#374151';
Chart.defaults.backgroundColor = 'rgba(59, 130, 246, 0.1)';

// Custom chart colors palette
window.chartColors = {
    production: 'rgb(34, 197, 94)',
    consumption: 'rgb(59, 130, 246)', 
    import: 'rgb(245, 158, 11)',
    export: 'rgb(16, 185, 129)',
    balance: 'rgb(168, 85, 247)',
    autarky: 'rgb(239, 68, 68)',
    selfConsumption: 'rgb(147, 51, 234)'
};

// Render Autarky & Self-Consumption Trends Chart
window.renderTrendsChart = (canvasId, labels, autarkyData, selfConsumptionData) => {
    const config = {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Autarky %',
                    data: autarkyData,
                    borderColor: 'rgb(59, 130, 246)',
                    backgroundColor: 'rgba(59, 130, 246, 0.1)',
                    borderWidth: 3,
                    tension: 0.4,
                    fill: true
                },
                {
                    label: 'Self-Consumption %',
                    data: selfConsumptionData,
                    borderColor: 'rgb(34, 197, 94)',
                    backgroundColor: 'rgba(34, 197, 94, 0.1)',
                    borderWidth: 3,
                    tension: 0.4,
                    fill: true
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': ' + context.parsed.y.toFixed(1) + '%';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    ticks: {
                        callback: function(value) {
                            return value + '%';
                        }
                    },
                    title: {
                        display: true,
                        text: 'Percentage (%)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Period'
                    }
                }
            },
            interaction: {
                mode: 'nearest',
                axis: 'x',
                intersect: false
            }
        }
    };

    window.createChart(canvasId, config);
};

console.log('Chart.js helper functions loaded successfully');