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

console.log('Chart.js helper functions loaded successfully');