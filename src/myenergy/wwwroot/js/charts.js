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

// Render Heatmap Chart (Matrix visualization)
window.renderHeatmapChart = (canvasId, data, maxValue, color) => {
    const labels = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    const hours = Array.from({length: 24}, (_, i) => `${i}:00`);
    
    // Transform flat array into matrix format for Chart.js Matrix
    const matrixData = [];
    for (let h = 0; h < 24; h++) {
        for (let d = 0; d < 7; d++) {
            const value = data[h * 7 + d];
            matrixData.push({
                x: labels[d],
                y: hours[h],
                v: value
            });
        }
    }

    const config = {
        type: 'bar',
        data: {
            labels: hours,
            datasets: labels.map((day, dayIdx) => ({
                label: day,
                data: Array.from({length: 24}, (_, h) => data[h * 7 + dayIdx]),
                backgroundColor: color.replace('rgb', 'rgba').replace(')', ', 0.6)'),
                borderColor: color,
                borderWidth: 1
            }))
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
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': ' + context.parsed.y.toFixed(2) + ' kWh';
                        }
                    }
                }
            },
            scales: {
                x: {
                    title: {
                        display: true,
                        text: 'Hour of Day'
                    },
                    stacked: false
                },
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Energy (kWh)'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Hourly Distribution Chart
window.renderHourlyDistributionChart = (canvasId, data, color) => {
    const hours = Array.from({length: 24}, (_, i) => `${i}:00`);
    
    const config = {
        type: 'bar',
        data: {
            labels: hours,
            datasets: [{
                label: 'Average by Hour',
                data: data,
                backgroundColor: color.replace('rgb', 'rgba').replace(')', ', 0.6)'),
                borderColor: color,
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return 'Average: ' + context.parsed.y.toFixed(2) + ' kWh';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Average Energy (kWh)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Hour of Day'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Day of Week Distribution Chart
window.renderDayOfWeekChart = (canvasId, labels, data, color) => {
    const config = {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Average by Day',
                data: data,
                backgroundColor: color.replace('rgb', 'rgba').replace(')', ', 0.6)'),
                borderColor: color,
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return 'Average: ' + context.parsed.y.toFixed(2) + ' kWh';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Average Energy (kWh)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Day of Week'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Scatter Chart for Weather Correlation
window.renderScatterChart = (canvasId, weatherValues, productions, weatherName, unit) => {
    const scatterData = weatherValues.map((w, i) => ({
        x: w,
        y: productions[i]
    }));

    const config = {
        type: 'scatter',
        data: {
            datasets: [{
                label: 'Production vs ' + weatherName,
                data: scatterData,
                backgroundColor: 'rgba(59, 130, 246, 0.6)',
                borderColor: 'rgb(59, 130, 246)',
                pointRadius: 5,
                pointHoverRadius: 7
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return weatherName + ': ' + context.parsed.x.toFixed(1) + ' ' + unit + 
                                   ', Production: ' + context.parsed.y.toFixed(2) + ' kWh';
                        }
                    }
                }
            },
            scales: {
                x: {
                    title: {
                        display: true,
                        text: weatherName + ' (' + unit + ')'
                    }
                },
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Solar Production (kWh)'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Timeline Chart with dual Y-axes
window.renderTimelineChart = (canvasId, dates, productions, weatherValues, weatherName) => {
    const config = {
        type: 'line',
        data: {
            labels: dates,
            datasets: [
                {
                    label: 'Production (kWh)',
                    data: productions,
                    borderColor: 'rgb(34, 197, 94)',
                    backgroundColor: 'rgba(34, 197, 94, 0.1)',
                    borderWidth: 2,
                    tension: 0.4,
                    fill: true,
                    yAxisID: 'y'
                },
                {
                    label: weatherName,
                    data: weatherValues,
                    borderColor: 'rgb(59, 130, 246)',
                    backgroundColor: 'rgba(59, 130, 246, 0.1)',
                    borderWidth: 2,
                    tension: 0.4,
                    fill: true,
                    yAxisID: 'y1'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                }
            },
            scales: {
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Production (kWh)'
                    }
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: weatherName
                    },
                    grid: {
                        drawOnChartArea: false
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Weather Ranges Bar Chart
window.renderWeatherRangesChart = (canvasId, labels, averages) => {
    const config = {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Average Production',
                data: averages,
                backgroundColor: 'rgba(34, 197, 94, 0.6)',
                borderColor: 'rgb(34, 197, 94)',
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return 'Avg Production: ' + context.parsed.y.toFixed(2) + ' kWh';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Average Production (kWh)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Weather Range'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Multi-Factor Correlation Radar/Bar Chart
window.renderMultiFactorChart = (canvasId, labels, correlations) => {
    // Convert correlations to absolute values for better visualization
    const colors = correlations.map(c => 
        c > 0.3 ? 'rgba(34, 197, 94, 0.6)' : 
        c < -0.3 ? 'rgba(239, 68, 68, 0.6)' : 
        'rgba(156, 163, 175, 0.6)'
    );
    
    const borderColors = correlations.map(c => 
        c > 0.3 ? 'rgb(34, 197, 94)' : 
        c < -0.3 ? 'rgb(239, 68, 68)' : 
        'rgb(156, 163, 175)'
    );

    const config = {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Correlation Coefficient',
                data: correlations,
                backgroundColor: colors,
                borderColor: borderColors,
                borderWidth: 2
            }]
        },
        options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            var corr = context.parsed.x;
                            var strength = Math.abs(corr) > 0.7 ? 'Strong' : 
                                         Math.abs(corr) > 0.4 ? 'Moderate' : 
                                         Math.abs(corr) > 0.2 ? 'Weak' : 'Very Weak';
                            return 'r = ' + corr.toFixed(3) + ' (' + strength + ')';
                        }
                    }
                }
            },
            scales: {
                x: {
                    min: -1,
                    max: 1,
                    title: {
                        display: true,
                        text: 'Correlation Coefficient (r)'
                    },
                    grid: {
                        color: function(context) {
                            if (context.tick.value === 0) {
                                return 'rgb(0, 0, 0)';
                            }
                            return 'rgba(0, 0, 0, 0.1)';
                        },
                        lineWidth: function(context) {
                            if (context.tick.value === 0) {
                                return 2;
                            }
                            return 1;
                        }
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Radar Chart for Monthly Comparisons
window.renderRadarChart = (canvasId, labels, datasets) => {
    const config = {
        type: 'radar',
        data: {
            labels: labels,
            datasets: datasets
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
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': ' + context.parsed.r.toFixed(1) + ' kWh';
                        }
                    }
                }
            },
            scales: {
                r: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            return value.toFixed(0);
                        }
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Seasonal Comparison Chart
window.renderSeasonalChart = (canvasId, seasons, productionData, consumptionData) => {
    const config = {
        type: 'bar',
        data: {
            labels: seasons,
            datasets: [
                {
                    label: 'Production',
                    data: productionData,
                    backgroundColor: 'rgba(25, 135, 84, 0.6)',
                    borderColor: 'rgb(25, 135, 84)',
                    borderWidth: 2
                },
                {
                    label: 'Consumption',
                    data: consumptionData,
                    backgroundColor: 'rgba(220, 53, 69, 0.6)',
                    borderColor: 'rgb(220, 53, 69)',
                    borderWidth: 2
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
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': ' + context.parsed.y.toLocaleString() + ' kWh';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Total Energy (kWh)'
                    },
                    ticks: {
                        callback: function(value) {
                            return value.toLocaleString();
                        }
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Season'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Year-over-Year Comparison Chart
window.renderYearOverYearChart = (canvasId, months, datasets) => {
    const config = {
        type: 'line',
        data: {
            labels: months,
            datasets: datasets
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
                            return context.dataset.label + ': ' + context.parsed.y.toLocaleString() + ' kWh';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Production (kWh)'
                    },
                    ticks: {
                        callback: function(value) {
                            return value.toLocaleString();
                        }
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Month'
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

// Render Cumulative Savings Chart with Payback Line
window.renderCumulativeSavingsChart = (canvasId, years, cumulativeSavings, systemCost) => {
    const config = {
        type: 'line',
        data: {
            labels: years,
            datasets: [
                {
                    label: 'Cumulative Savings',
                    data: cumulativeSavings,
                    borderColor: 'rgb(25, 135, 84)',
                    backgroundColor: 'rgba(25, 135, 84, 0.1)',
                    borderWidth: 3,
                    tension: 0.4,
                    fill: true,
                    pointRadius: 6,
                    pointHoverRadius: 8
                },
                {
                    label: 'System Cost (Payback Target)',
                    data: Array(years.length).fill(systemCost),
                    borderColor: 'rgb(220, 53, 69)',
                    backgroundColor: 'rgba(220, 53, 69, 0.1)',
                    borderWidth: 2,
                    borderDash: [10, 5],
                    fill: false,
                    pointRadius: 0
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
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': €' + context.parsed.y.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2});
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Amount (€)'
                    },
                    ticks: {
                        callback: function(value) {
                            return '€' + value.toLocaleString();
                        }
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Year'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Monthly Savings Breakdown Chart
window.renderCostSavingsMonthlySavingsChart = (canvasId, labels, savings, importCosts) => {
    const config = {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Savings',
                    data: savings,
                    backgroundColor: 'rgba(25, 135, 84, 0.6)',
                    borderColor: 'rgb(25, 135, 84)',
                    borderWidth: 2
                },
                {
                    label: 'Grid Import Costs',
                    data: importCosts,
                    backgroundColor: 'rgba(220, 53, 69, 0.6)',
                    borderColor: 'rgb(220, 53, 69)',
                    borderWidth: 2
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
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': €' + context.parsed.y.toFixed(2);
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Amount (€)'
                    },
                    ticks: {
                        callback: function(value) {
                            return '€' + value.toFixed(0);
                        }
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Month'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Cost Comparison Chart (Doughnut)
window.renderCostComparisonChart = (canvasId, costWithoutSolar, costWithSolar, savings) => {
    const config = {
        type: 'doughnut',
        data: {
            labels: ['Amount Saved', 'Actual Cost Paid'],
            datasets: [{
                data: [savings, costWithSolar],
                backgroundColor: [
                    'rgba(25, 135, 84, 0.8)',
                    'rgba(220, 53, 69, 0.8)'
                ],
                borderColor: [
                    'rgb(25, 135, 84)',
                    'rgb(220, 53, 69)'
                ],
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: true,
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            var label = context.label || '';
                            var value = context.parsed;
                            var total = context.dataset.data.reduce((a, b) => a + b, 0);
                            var percentage = ((value / total) * 100).toFixed(1);
                            return label + ': €' + value.toLocaleString(undefined, {minimumFractionDigits: 2, maximumFractionDigits: 2}) + ' (' + percentage + '%)';
                        }
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Import/Export Balance Chart
window.renderBalanceChart = (canvasId, labels, imports, exports) => {
    const config = {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Grid Import',
                    data: imports,
                    backgroundColor: 'rgba(13, 110, 253, 0.6)',
                    borderColor: 'rgb(13, 110, 253)',
                    borderWidth: 2
                },
                {
                    label: 'Grid Export',
                    data: exports,
                    backgroundColor: 'rgba(255, 193, 7, 0.6)',
                    borderColor: 'rgb(255, 193, 7)',
                    borderWidth: 2
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
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': ' + Math.abs(context.parsed.y).toFixed(1) + ' kWh';
                        }
                    }
                }
            },
            scales: {
                y: {
                    title: {
                        display: true,
                        text: 'Energy (kWh)'
                    },
                    ticks: {
                        callback: function(value) {
                            return Math.abs(value).toFixed(0);
                        }
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Month'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Daily Flow Pattern Chart
window.renderDailyFlowChart = (canvasId, avgProduction, avgConsumption, avgImport, avgExport) => {
    const config = {
        type: 'bar',
        data: {
            labels: ['Production', 'Consumption', 'Import', 'Export'],
            datasets: [{
                label: 'Average Daily Energy',
                data: [avgProduction, avgConsumption, avgImport, avgExport],
                backgroundColor: [
                    'rgba(25, 135, 84, 0.6)',
                    'rgba(220, 53, 69, 0.6)',
                    'rgba(13, 110, 253, 0.6)',
                    'rgba(255, 193, 7, 0.6)'
                ],
                borderColor: [
                    'rgb(25, 135, 84)',
                    'rgb(220, 53, 69)',
                    'rgb(13, 110, 253)',
                    'rgb(255, 193, 7)'
                ],
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return context.label + ': ' + context.parsed.y.toFixed(2) + ' kWh/day';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Average Energy (kWh/day)'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Energy Source Distribution Chart
window.renderEnergySourceChart = (canvasId, selfConsumption, gridImport) => {
    const config = {
        type: 'doughnut',
        data: {
            labels: ['Self-Consumption (Solar)', 'Grid Import'],
            datasets: [{
                data: [selfConsumption, gridImport],
                backgroundColor: [
                    'rgba(255, 193, 7, 0.8)',
                    'rgba(13, 110, 253, 0.8)'
                ],
                borderColor: [
                    'rgb(255, 193, 7)',
                    'rgb(13, 110, 253)'
                ],
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: true,
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            var label = context.label || '';
                            var value = context.parsed;
                            var total = context.dataset.data.reduce((a, b) => a + b, 0);
                            var percentage = ((value / total) * 100).toFixed(1);
                            return label + ': ' + value.toLocaleString(undefined, {minimumFractionDigits: 0, maximumFractionDigits: 0}) + ' kWh (' + percentage + '%)';
                        }
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Day Type Comparison Chart (Grouped Bar)
window.renderDayTypeComparisonChart = (canvasId, labels, type1Values, type2Values, type1Label, type2Label) => {
    const config = {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: type1Label,
                    data: type1Values,
                    backgroundColor: 'rgba(13, 110, 253, 0.6)',
                    borderColor: 'rgb(13, 110, 253)',
                    borderWidth: 2
                },
                {
                    label: type2Label,
                    data: type2Values,
                    backgroundColor: 'rgba(25, 135, 84, 0.6)',
                    borderColor: 'rgb(25, 135, 84)',
                    borderWidth: 2
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
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': ' + context.parsed.y.toFixed(2) + ' kWh';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Energy (kWh)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Metric'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Seasonal Patterns Chart
window.renderSeasonalPatternsChart = (canvasId, seasons, productions, consumptions) => {
    const config = {
        type: 'bar',
        data: {
            labels: seasons,
            datasets: [
                {
                    label: 'Avg Production',
                    data: productions,
                    backgroundColor: 'rgba(25, 135, 84, 0.6)',
                    borderColor: 'rgb(25, 135, 84)',
                    borderWidth: 2
                },
                {
                    label: 'Avg Consumption',
                    data: consumptions,
                    backgroundColor: 'rgba(220, 53, 69, 0.6)',
                    borderColor: 'rgb(220, 53, 69)',
                    borderWidth: 2
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
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': ' + context.parsed.y.toFixed(1) + ' kWh/day';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Average Daily Energy (kWh)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Season'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Efficiency Trend Chart
window.renderEfficiencyTrendChart = (canvasId, labels, values, metricTitle) => {
    const config = {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: metricTitle,
                data: values,
                borderColor: 'rgb(13, 110, 253)',
                backgroundColor: 'rgba(13, 110, 253, 0.1)',
                borderWidth: 3,
                tension: 0.4,
                fill: true,
                pointRadius: 4,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return metricTitle + ': ' + context.parsed.y.toFixed(1) + '%';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    title: {
                        display: true,
                        text: 'Percentage (%)'
                    },
                    ticks: {
                        callback: function(value) {
                            return value + '%';
                        }
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Month'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Seasonal Efficiency Comparison
window.renderSeasonalEfficiencyChart = (canvasId, seasons, cfValues, prValues) => {
    const config = {
        type: 'bar',
        data: {
            labels: seasons,
            datasets: [
                {
                    label: 'Capacity Factor',
                    data: cfValues,
                    backgroundColor: 'rgba(25, 135, 84, 0.6)',
                    borderColor: 'rgb(25, 135, 84)',
                    borderWidth: 2
                },
                {
                    label: 'Performance Ratio',
                    data: prValues,
                    backgroundColor: 'rgba(13, 110, 253, 0.6)',
                    borderColor: 'rgb(13, 110, 253)',
                    borderWidth: 2
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
                    title: {
                        display: true,
                        text: 'Percentage (%)'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Health Score Distribution
window.renderHealthScoreDistribution = (canvasId, scores) => {
    const config = {
        type: 'doughnut',
        data: {
            labels: ['Excellent (80-100)', 'Good (60-80)', 'Fair (40-60)', 'Poor (0-40)'],
            datasets: [{
                data: scores,
                backgroundColor: [
                    'rgba(25, 135, 84, 0.8)',
                    'rgba(13, 110, 253, 0.8)',
                    'rgba(255, 193, 7, 0.8)',
                    'rgba(220, 53, 69, 0.8)'
                ],
                borderColor: [
                    'rgb(25, 135, 84)',
                    'rgb(13, 110, 253)',
                    'rgb(255, 193, 7)',
                    'rgb(220, 53, 69)'
                ],
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: true,
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return context.label + ': ' + context.parsed + ' days';
                        }
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Forecast Chart with Confidence Intervals
window.renderForecastChart = (canvasId, labels, predictions, lowerBounds, upperBounds) => {
    const config = {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Predicted Production',
                    data: predictions,
                    borderColor: 'rgb(13, 110, 253)',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    borderWidth: 3,
                    tension: 0.4,
                    fill: false,
                    pointRadius: 5,
                    pointHoverRadius: 7
                },
                {
                    label: 'Upper Bound',
                    data: upperBounds,
                    borderColor: 'rgba(13, 110, 253, 0.3)',
                    backgroundColor: 'rgba(13, 110, 253, 0.05)',
                    borderWidth: 1,
                    borderDash: [5, 5],
                    tension: 0.4,
                    fill: '+1',
                    pointRadius: 0
                },
                {
                    label: 'Lower Bound',
                    data: lowerBounds,
                    borderColor: 'rgba(13, 110, 253, 0.3)',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    borderWidth: 1,
                    borderDash: [5, 5],
                    tension: 0.4,
                    fill: false,
                    pointRadius: 0
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
                            return context.dataset.label + ': ' + context.parsed.y.toFixed(1) + ' kWh';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Production (kWh)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Yearly Trend Forecast
window.renderYearlyTrendForecast = (canvasId, years, productions, historicalCount) => {
    const config = {
        type: 'bar',
        data: {
            labels: years,
            datasets: [{
                label: 'Production',
                data: productions,
                backgroundColor: productions.map((_, i) => 
                    i < historicalCount ? 'rgba(25, 135, 84, 0.6)' : 'rgba(13, 110, 253, 0.6)'
                ),
                borderColor: productions.map((_, i) => 
                    i < historicalCount ? 'rgb(25, 135, 84)' : 'rgb(13, 110, 253)'
                ),
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            var label = context.dataIndex < historicalCount ? 'Historical' : 'Forecast';
                            return label + ': ' + context.parsed.y.toLocaleString() + ' kWh';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Annual Production (kWh)'
                    },
                    ticks: {
                        callback: function(value) {
                            return value.toLocaleString();
                        }
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Render Production Distribution Histogram
window.renderProductionDistribution = (canvasId, labels, counts) => {
    const config = {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Number of Days',
                data: counts,
                backgroundColor: 'rgba(13, 110, 253, 0.6)',
                borderColor: 'rgb(13, 110, 253)',
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return context.parsed.y + ' days';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Number of Days'
                    },
                    ticks: {
                        precision: 0
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Production Range (kWh)'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Battery Simulation Charts

window.renderCostComparisonChart = (costs) => {
    const canvasId = 'costComparisonChart';
    
    const config = {
        type: 'bar',
        data: {
            labels: ['Fixed (No Battery)', 'Dynamic (No Battery)', 'Fixed + Battery', 'Dynamic + Battery'],
            datasets: [{
                label: 'Annual Cost (€)',
                data: costs,
                backgroundColor: [
                    'rgba(220, 53, 69, 0.8)',   // danger
                    'rgba(255, 193, 7, 0.8)',   // warning
                    'rgba(13, 110, 253, 0.8)',  // info
                    'rgba(25, 135, 84, 0.8)'    // success
                ],
                borderColor: [
                    'rgb(220, 53, 69)',
                    'rgb(255, 193, 7)',
                    'rgb(13, 110, 253)',
                    'rgb(25, 135, 84)'
                ],
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: (context) => `€${context.parsed.y.toFixed(2)}`
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Annual Cost (€)'
                    },
                    ticks: {
                        callback: (value) => `€${value}`
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

window.renderSavingsBreakdownChart = (savings) => {
    const canvasId = 'savingsBreakdownChart';
    
    const config = {
        type: 'doughnut',
        data: {
            labels: [
                'Dynamic Pricing Benefit',
                'Battery Benefit (Fixed)',
                'Battery Benefit (Dynamic)',
                'Total Savings'
            ],
            datasets: [{
                data: savings,
                backgroundColor: [
                    'rgba(255, 193, 7, 0.8)',
                    'rgba(13, 110, 253, 0.8)',
                    'rgba(25, 135, 84, 0.8)',
                    'rgba(111, 66, 193, 0.8)'
                ],
                borderColor: [
                    'rgb(255, 193, 7)',
                    'rgb(13, 110, 253)',
                    'rgb(25, 135, 84)',
                    'rgb(111, 66, 193)'
                ],
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => {
                            const label = context.label || '';
                            const value = context.parsed;
                            return `${label}: €${value.toFixed(2)}`;
                        }
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

window.renderCumulativeCostChart = (labels, fixedCosts, dynamicNoBatCosts, dynamicWithBatCosts) => {
    const canvasId = 'cumulativeCostChart';
    
    const config = {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Fixed Tariff (No Battery)',
                    data: fixedCosts,
                    borderColor: 'rgb(220, 53, 69)',
                    backgroundColor: 'rgba(220, 53, 69, 0.1)',
                    borderWidth: 2,
                    tension: 0.1
                },
                {
                    label: 'Dynamic Tariff (No Battery)',
                    data: dynamicNoBatCosts,
                    borderColor: 'rgb(255, 193, 7)',
                    backgroundColor: 'rgba(255, 193, 7, 0.1)',
                    borderWidth: 2,
                    tension: 0.1
                },
                {
                    label: 'Dynamic Tariff + Battery',
                    data: dynamicWithBatCosts,
                    borderColor: 'rgb(25, 135, 84)',
                    backgroundColor: 'rgba(25, 135, 84, 0.1)',
                    borderWidth: 3,
                    tension: 0.1
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => `${context.dataset.label}: €${context.parsed.y.toFixed(2)}`
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Cumulative Cost (€)'
                    },
                    ticks: {
                        callback: (value) => `€${value}`
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    },
                    ticks: {
                        maxTicksLimit: 20
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

window.renderBatterySocChart = (labels, socLevels, capacity) => {
    const canvasId = 'batterySocChart';
    
    const config = {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Battery State of Charge (%)',
                data: socLevels,
                borderColor: 'rgb(13, 110, 253)',
                backgroundColor: 'rgba(13, 110, 253, 0.2)',
                borderWidth: 2,
                fill: true,
                tension: 0.4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: true
                },
                tooltip: {
                    callbacks: {
                        label: (context) => `SoC: ${context.parsed.y.toFixed(1)}%`,
                        afterLabel: (context) => {
                            const kwh = (context.parsed.y / 100 * capacity).toFixed(2);
                            return `(${kwh} kWh / ${capacity} kWh)`;
                        }
                    }
                },
                annotation: {
                    annotations: {
                        line1: {
                            type: 'line',
                            yMin: 90,
                            yMax: 90,
                            borderColor: 'rgb(25, 135, 84)',
                            borderWidth: 2,
                            borderDash: [5, 5],
                            label: {
                                display: true,
                                content: 'Max Capacity (90%)',
                                position: 'end'
                            }
                        },
                        line2: {
                            type: 'line',
                            yMin: 10,
                            yMax: 10,
                            borderColor: 'rgb(220, 53, 69)',
                            borderWidth: 2,
                            borderDash: [5, 5],
                            label: {
                                display: true,
                                content: 'Min Capacity (10%)',
                                position: 'end'
                            }
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    title: {
                        display: true,
                        text: 'State of Charge (%)'
                    },
                    ticks: {
                        callback: (value) => `${value}%`
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Time'
                    },
                    ticks: {
                        maxTicksLimit: 30
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// Daily Cost Analysis Charts

window.renderDailyCostsChart = (labels, fixedCosts, dynamicNoBatCosts, dynamicWithBatCosts) => {
    const canvasId = 'mainDailyChart';
    
    const config = {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Fixed (No Battery)',
                    data: fixedCosts,
                    borderColor: 'rgb(220, 53, 69)',
                    backgroundColor: 'rgba(220, 53, 69, 0.1)',
                    borderWidth: 2,
                    tension: 0.1,
                    pointRadius: 2
                },
                {
                    label: 'Dynamic (No Battery)',
                    data: dynamicNoBatCosts,
                    borderColor: 'rgb(255, 193, 7)',
                    backgroundColor: 'rgba(255, 193, 7, 0.1)',
                    borderWidth: 2,
                    tension: 0.1,
                    pointRadius: 2
                },
                {
                    label: 'Dynamic + Battery',
                    data: dynamicWithBatCosts,
                    borderColor: 'rgb(25, 135, 84)',
                    backgroundColor: 'rgba(25, 135, 84, 0.1)',
                    borderWidth: 3,
                    tension: 0.1,
                    pointRadius: 3
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => `${context.dataset.label}: €${context.parsed.y.toFixed(2)}`
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Daily Cost (€)'
                    },
                    ticks: {
                        callback: (value) => `€${value}`
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    },
                    ticks: {
                        maxTicksLimit: 30
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

window.renderCumulativeDailyCostsChart = (labels, cumFixed, cumDynNoBat, cumDynWithBat) => {
    const canvasId = 'mainDailyChart';
    
    const config = {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Fixed (No Battery)',
                    data: cumFixed,
                    borderColor: 'rgb(220, 53, 69)',
                    backgroundColor: 'rgba(220, 53, 69, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.1
                },
                {
                    label: 'Dynamic (No Battery)',
                    data: cumDynNoBat,
                    borderColor: 'rgb(255, 193, 7)',
                    backgroundColor: 'rgba(255, 193, 7, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.1
                },
                {
                    label: 'Dynamic + Battery',
                    data: cumDynWithBat,
                    borderColor: 'rgb(25, 135, 84)',
                    backgroundColor: 'rgba(25, 135, 84, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.1
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => `${context.dataset.label}: €${context.parsed.y.toFixed(2)}`
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Cumulative Cost (€)'
                    },
                    ticks: {
                        callback: (value) => `€${value}`
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    },
                    ticks: {
                        maxTicksLimit: 30
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

window.renderDailySavingsChart = (labels, totalSavings, batterySavings) => {
    const canvasId = 'mainDailyChart';
    
    const config = {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Total Savings (vs Fixed)',
                    data: totalSavings,
                    backgroundColor: 'rgba(25, 135, 84, 0.7)',
                    borderColor: 'rgb(25, 135, 84)',
                    borderWidth: 1
                },
                {
                    label: 'Battery Benefit',
                    data: batterySavings,
                    backgroundColor: 'rgba(13, 110, 253, 0.7)',
                    borderColor: 'rgb(13, 110, 253)',
                    borderWidth: 1
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => `${context.dataset.label}: €${context.parsed.y.toFixed(2)}`
                    }
                }
            },
            scales: {
                y: {
                    title: {
                        display: true,
                        text: 'Daily Savings (€)'
                    },
                    ticks: {
                        callback: (value) => `€${value}`
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    },
                    ticks: {
                        maxTicksLimit: 30
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

window.renderAllMetricsChart = (labels, fixedCosts, dynamicNoBatCosts, dynamicWithBatCosts, totalSavings, batterySavings) => {
    const canvasId = 'mainDailyChart';
    
    const config = {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Fixed (No Battery)',
                    data: fixedCosts,
                    borderColor: 'rgb(220, 53, 69)',
                    backgroundColor: 'rgba(220, 53, 69, 0.1)',
                    borderWidth: 2,
                    yAxisID: 'y',
                    tension: 0.1
                },
                {
                    label: 'Dynamic (No Battery)',
                    data: dynamicNoBatCosts,
                    borderColor: 'rgb(255, 193, 7)',
                    backgroundColor: 'rgba(255, 193, 7, 0.1)',
                    borderWidth: 2,
                    yAxisID: 'y',
                    tension: 0.1
                },
                {
                    label: 'Dynamic + Battery',
                    data: dynamicWithBatCosts,
                    borderColor: 'rgb(25, 135, 84)',
                    backgroundColor: 'rgba(25, 135, 84, 0.1)',
                    borderWidth: 3,
                    yAxisID: 'y',
                    tension: 0.1
                },
                {
                    label: 'Total Savings',
                    data: totalSavings,
                    borderColor: 'rgb(111, 66, 193)',
                    backgroundColor: 'rgba(111, 66, 193, 0.1)',
                    borderWidth: 2,
                    borderDash: [5, 5],
                    yAxisID: 'y1',
                    tension: 0.1
                },
                {
                    label: 'Battery Benefit',
                    data: batterySavings,
                    borderColor: 'rgb(13, 110, 253)',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    borderWidth: 2,
                    borderDash: [5, 5],
                    yAxisID: 'y1',
                    tension: 0.1
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => `${context.dataset.label}: €${context.parsed.y.toFixed(2)}`
                    }
                }
            },
            scales: {
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    title: {
                        display: true,
                        text: 'Daily Cost (€)'
                    },
                    ticks: {
                        callback: (value) => `€${value}`
                    }
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    title: {
                        display: true,
                        text: 'Daily Savings (€)'
                    },
                    ticks: {
                        callback: (value) => `€${value}`
                    },
                    grid: {
                        drawOnChartArea: false
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    },
                    ticks: {
                        maxTicksLimit: 30
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

window.renderMonthlySummaryChart = (monthNames, fixedCosts, dynamicNoBatCosts, dynamicWithBatCosts) => {
    const canvasId = 'monthlySummaryChart';
    
    const config = {
        type: 'bar',
        data: {
            labels: monthNames,
            datasets: [
                {
                    label: 'Fixed (No Battery)',
                    data: fixedCosts,
                    backgroundColor: 'rgba(220, 53, 69, 0.7)',
                    borderColor: 'rgb(220, 53, 69)',
                    borderWidth: 1
                },
                {
                    label: 'Dynamic (No Battery)',
                    data: dynamicNoBatCosts,
                    backgroundColor: 'rgba(255, 193, 7, 0.7)',
                    borderColor: 'rgb(255, 193, 7)',
                    borderWidth: 1
                },
                {
                    label: 'Dynamic + Battery',
                    data: dynamicWithBatCosts,
                    backgroundColor: 'rgba(25, 135, 84, 0.7)',
                    borderColor: 'rgb(25, 135, 84)',
                    borderWidth: 2
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => `${context.dataset.label}: €${context.parsed.y.toFixed(2)}`
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Monthly Cost (€)'
                    },
                    ticks: {
                        callback: (value) => `€${value}`
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Month'
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

window.renderBatteryPerformanceChart = (labels, batteryCharged, batteryDischarged, batterySavings, batteryCapacity) => {
    const canvasId = 'batteryPerformanceChart';
    
    // Create capacity reference line data (array filled with capacity value)
    const capacityLine = new Array(labels.length).fill(batteryCapacity);
    
    const config = {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Daily Energy Charged (kWh)',
                    data: batteryCharged,
                    backgroundColor: 'rgba(13, 110, 253, 0.6)',
                    borderColor: 'rgb(13, 110, 253)',
                    borderWidth: 1,
                    yAxisID: 'y'
                },
                {
                    label: 'Daily Energy Discharged (kWh)',
                    data: batteryDischarged,
                    backgroundColor: 'rgba(255, 193, 7, 0.6)',
                    borderColor: 'rgb(255, 193, 7)',
                    borderWidth: 1,
                    yAxisID: 'y'
                },
                {
                    label: `Battery Capacity (${batteryCapacity} kWh)`,
                    data: capacityLine,
                    type: 'line',
                    borderColor: 'rgba(220, 53, 69, 0.5)',
                    backgroundColor: 'transparent',
                    borderWidth: 2,
                    borderDash: [5, 5],
                    yAxisID: 'y',
                    pointRadius: 0,
                    order: 0
                },
                {
                    label: 'Daily Battery Savings (€)',
                    data: batterySavings,
                    type: 'line',
                    borderColor: 'rgb(25, 135, 84)',
                    backgroundColor: 'rgba(25, 135, 84, 0.1)',
                    borderWidth: 2,
                    yAxisID: 'y1',
                    tension: 0.1
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => {
                            const label = context.dataset.label || '';
                            const value = context.parsed.y;
                            if (label.includes('€')) {
                                return `${label}: €${value.toFixed(2)}`;
                            } else if (label.includes('Capacity')) {
                                return `${label} (Reference)`;
                            } else {
                                return `${label}: ${value.toFixed(2)} kWh`;
                            }
                        }
                    }
                }
            },
            scales: {
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    title: {
                        display: true,
                        text: 'Daily Energy Throughput (kWh)'
                    },
                    ticks: {
                        callback: (value) => `${value} kWh`
                    }
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    title: {
                        display: true,
                        text: 'Savings (€)'
                    },
                    ticks: {
                        callback: (value) => `€${value}`
                    },
                    grid: {
                        drawOnChartArea: false
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    },
                    ticks: {
                        maxTicksLimit: 30
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

// ROI Analysis Charts
window.renderRoiChart = (labels, solarNet, batteryNet, combinedNet, includeSolar, includeBattery) => {
    const canvasId = 'roiChart';
    
    const datasets = [];
    
    if (includeSolar) {
        datasets.push({
            label: 'Solar Net Position',
            data: solarNet,
            borderColor: 'rgb(255, 193, 7)',
            backgroundColor: 'rgba(255, 193, 7, 0.1)',
            borderWidth: 2,
            fill: true,
            tension: 0.1
        });
    }
    
    if (includeBattery) {
        datasets.push({
            label: 'Battery Net Position',
            data: batteryNet,
            borderColor: 'rgb(25, 135, 84)',
            backgroundColor: 'rgba(25, 135, 84, 0.1)',
            borderWidth: 2,
            fill: true,
            tension: 0.1
        });
    }
    
    if (includeSolar || includeBattery) {
        datasets.push({
            label: 'Combined Net Position',
            data: combinedNet,
            borderColor: 'rgb(13, 110, 253)',
            backgroundColor: 'rgba(13, 110, 253, 0.1)',
            borderWidth: 3,
            fill: true,
            tension: 0.1
        });
    }
    
    // Add break-even line at y=0
    datasets.push({
        label: 'Break-Even Point',
        data: new Array(labels.length).fill(0),
        borderColor: 'rgba(220, 53, 69, 0.5)',
        backgroundColor: 'transparent',
        borderWidth: 2,
        borderDash: [10, 5],
        pointRadius: 0,
        fill: false
    });
    
    const config = {
        type: 'line',
        data: {
            labels: labels,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => {
                            const label = context.dataset.label || '';
                            const value = context.parsed.y;
                            if (label.includes('Break-Even')) {
                                return 'Break-Even';
                            }
                            return `${label}: €${value.toFixed(2)}`;
                        }
                    }
                }
            },
            scales: {
                y: {
                    title: {
                        display: true,
                        text: 'Net Position (€)'
                    },
                    ticks: {
                        callback: (value) => `€${value.toLocaleString()}`
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Time Period'
                    },
                    ticks: {
                        maxTicksLimit: 24
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

window.renderMonthlySavingsChart = (labels, solarSavings, batterySavings, includeSolar, includeBattery) => {
    const canvasId = 'monthlySavingsChart';
    
    const datasets = [];
    
    if (includeSolar) {
        datasets.push({
            label: 'Solar Savings',
            data: solarSavings,
            backgroundColor: 'rgba(255, 193, 7, 0.8)',
            borderColor: 'rgb(255, 193, 7)',
            borderWidth: 1
        });
    }
    
    if (includeBattery) {
        datasets.push({
            label: 'Battery Savings',
            data: batterySavings,
            backgroundColor: 'rgba(25, 135, 84, 0.8)',
            borderColor: 'rgb(25, 135, 84)',
            borderWidth: 1
        });
    }
    
    const config = {
        type: 'bar',
        data: {
            labels: labels,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => {
                            const label = context.dataset.label || '';
                            const value = context.parsed.y;
                            return `${label}: €${value.toFixed(2)}`;
                        }
                    }
                }
            },
            scales: {
                y: {
                    stacked: includeSolar && includeBattery,
                    title: {
                        display: true,
                        text: 'Monthly Savings (€)'
                    },
                    ticks: {
                        callback: (value) => `€${value}`
                    }
                },
                x: {
                    stacked: includeSolar && includeBattery,
                    title: {
                        display: true,
                        text: 'Month'
                    },
                    ticks: {
                        maxTicksLimit: 24
                    }
                }
            }
        }
    };

    window.createChart(canvasId, config);
};

console.log('Chart.js helper functions loaded successfully');