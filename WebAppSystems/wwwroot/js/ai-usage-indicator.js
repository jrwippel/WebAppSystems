/**
 * Componente para mostrar o status de uso da IA
 */
class AIUsageIndicator {
    constructor(containerId) {
        this.containerId = containerId;
        this.init();
    }

    init() {
        this.createIndicator();
        this.loadUsageStats();
        
        // Atualizar a cada 30 segundos
        setInterval(() => {
            this.loadUsageStats();
        }, 30000);
    }

    createIndicator() {
        const container = document.getElementById(this.containerId);
        if (!container) return;

        container.innerHTML = `
            <div class="ai-usage-indicator">
                <div class="card card-outline card-info">
                    <div class="card-header">
                        <h3 class="card-title">
                            <i class="fas fa-robot"></i> Status de Uso da IA
                        </h3>
                    </div>
                    <div class="card-body">
                        <div class="row">
                            <div class="col-md-4">
                                <div class="info-box">
                                    <span class="info-box-icon bg-info">
                                        <i class="fas fa-calendar-day"></i>
                                    </span>
                                    <div class="info-box-content">
                                        <span class="info-box-text">Usado Hoje</span>
                                        <span class="info-box-number" id="usedToday">-</span>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <div class="info-box">
                                    <span class="info-box-icon bg-success">
                                        <i class="fas fa-check-circle"></i>
                                    </span>
                                    <div class="info-box-content">
                                        <span class="info-box-text">Limite Diário</span>
                                        <span class="info-box-number" id="dailyLimit">-</span>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <div class="info-box">
                                    <span class="info-box-icon" id="remainingIcon">
                                        <i class="fas fa-battery-three-quarters"></i>
                                    </span>
                                    <div class="info-box-content">
                                        <span class="info-box-text">Restante</span>
                                        <span class="info-box-number" id="remaining">-</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                        
                        <div class="progress mb-3">
                            <div class="progress-bar" id="usageProgressBar" role="progressbar" 
                                 style="width: 0%" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100">
                            </div>
                        </div>
                        
                        <div class="alert" id="usageAlert" style="display: none;">
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    loadUsageStats() {
        // Determinar qual endpoint usar baseado na página atual
        let endpoint = '/DocumentAnalysis/GetAIUsageStats';
        if (window.location.pathname.includes('PainelGestao')) {
            endpoint = '/PainelGestao/GetAIUsageStats';
        }

        fetch(endpoint)
            .then(response => response.json())
            .then(data => {
                if (data.success || data.today) {
                    this.updateIndicator(data.data || data);
                } else {
                    this.showError('Erro ao carregar estatísticas de uso');
                }
            })
            .catch(error => {
                console.error('Erro ao carregar estatísticas de uso:', error);
                this.showError('Erro de conexão');
            });
    }

    updateIndicator(stats) {
        const today = stats.today || stats.Today;
        if (!today) return;

        const used = today.used || today.Used;
        const limit = today.limit || today.Limit;
        const remaining = today.remaining || today.Remaining;

        // Atualizar números
        document.getElementById('usedToday').textContent = used;
        document.getElementById('dailyLimit').textContent = limit;
        document.getElementById('remaining').textContent = remaining;

        // Calcular porcentagem
        const percentage = limit > 0 ? (used / limit) * 100 : 0;
        
        // Atualizar barra de progresso
        const progressBar = document.getElementById('usageProgressBar');
        progressBar.style.width = percentage + '%';
        progressBar.setAttribute('aria-valuenow', percentage);

        // Atualizar cores baseado no uso
        const remainingIcon = document.getElementById('remainingIcon');
        const alert = document.getElementById('usageAlert');

        if (remaining <= 0) {
            // Limite atingido
            progressBar.className = 'progress-bar bg-danger';
            remainingIcon.className = 'info-box-icon bg-danger';
            remainingIcon.innerHTML = '<i class="fas fa-ban"></i>';
            
            alert.className = 'alert alert-danger';
            alert.innerHTML = `
                <div style="text-align: center;">
                    <i class="fas fa-exclamation-triangle" style="font-size: 1.5rem; margin-bottom: 0.5rem;"></i>
                    <h5 style="margin-bottom: 0.5rem;"><strong>Limite Diário Atingido!</strong></h5>
                    <p style="margin-bottom: 1rem;">Você não pode mais usar a IA hoje.</p>
                    <div style="background: rgba(255,255,255,0.9); border-radius: 6px; padding: 0.75rem; margin-bottom: 1rem;">
                        <strong>💡 Para continuar:</strong><br>
                        • Aguarde até amanhã (reseta às 00:00)<br>
                        • Ou contate o administrador para upgrade
                    </div>
                    <small style="opacity: 0.8;">Limite atual: ${limit} consultas por dia</small>
                </div>
            `;
            alert.style.display = 'block';
        } else if (remaining <= 2) {
            // Próximo do limite
            progressBar.className = 'progress-bar bg-warning';
            remainingIcon.className = 'info-box-icon bg-warning';
            remainingIcon.innerHTML = '<i class="fas fa-battery-quarter"></i>';
            
            alert.className = 'alert alert-warning';
            alert.innerHTML = `<i class="fas fa-exclamation-circle"></i> <strong>Atenção!</strong> Você tem apenas ${remaining} consulta(s) restante(s) hoje.`;
            alert.style.display = 'block';
        } else {
            // Normal
            progressBar.className = 'progress-bar bg-success';
            remainingIcon.className = 'info-box-icon bg-success';
            
            if (percentage < 25) {
                remainingIcon.innerHTML = '<i class="fas fa-battery-full"></i>';
            } else if (percentage < 50) {
                remainingIcon.innerHTML = '<i class="fas fa-battery-three-quarters"></i>';
            } else if (percentage < 75) {
                remainingIcon.innerHTML = '<i class="fas fa-battery-half"></i>';
            } else {
                remainingIcon.innerHTML = '<i class="fas fa-battery-quarter"></i>';
            }
            
            alert.style.display = 'none';
        }
    }

    showError(message) {
        const alert = document.getElementById('usageAlert');
        if (alert) {
            alert.className = 'alert alert-danger';
            alert.innerHTML = `<i class="fas fa-exclamation-triangle"></i> ${message}`;
            alert.style.display = 'block';
        }
    }

    // Método para verificar se pode usar IA antes de fazer uma requisição
    static async canUseAI() {
        try {
            let endpoint = '/DocumentAnalysis/GetAIUsageStats';
            if (window.location.pathname.includes('PainelGestao')) {
                endpoint = '/PainelGestao/GetAIUsageStats';
            }

            const response = await fetch(endpoint);
            const data = await response.json();
            
            if (data.success || data.today) {
                const today = data.data?.today || data.today || data.Today;
                const remaining = today?.remaining || today?.Remaining || 0;
                
                if (remaining <= 0) {
                    // Mostrar mensagem mais detalhada
                    if (typeof Swal !== 'undefined') {
                        // Se SweetAlert estiver disponível, usar ele
                        Swal.fire({
                            icon: 'warning',
                            title: 'Limite Diário Atingido!',
                            html: `
                                <p>Você atingiu o limite de <strong>10 consultas de IA</strong> por dia.</p>
                                <hr>
                                <div style="background: #fff3cd; border: 1px solid #ffeaa7; border-radius: 8px; padding: 1rem; margin: 1rem 0; text-align: left;">
                                    <strong>💡 Para continuar usando a IA:</strong><br>
                                    • Aguarde até amanhã (limite reseta automaticamente às 00:00)<br>
                                    • Ou entre em contato com o administrador para upgrade do plano
                                </div>
                            `,
                            confirmButtonText: 'Entendi',
                            confirmButtonColor: '#667eea'
                        });
                    } else {
                        // Fallback para toastr
                        if (typeof toastr !== 'undefined') {
                            toastr.error('🚫 Limite diário de 10 consultas de IA atingido! Aguarde até amanhã ou entre em contato com o administrador para upgrade do plano.', 'Limite Atingido', {
                                timeOut: 8000,
                                extendedTimeOut: 3000
                            });
                        } else {
                            alert('🚫 Limite diário de 10 consultas de IA atingido! Aguarde até amanhã ou entre em contato com o administrador para upgrade do plano.');
                        }
                    }
                    return false;
                }
                
                return true;
            }
            
            return false;
        } catch (error) {
            console.error('Erro ao verificar limite de IA:', error);
            if (typeof toastr !== 'undefined') {
                toastr.error('Erro ao verificar limite de uso da IA');
            } else {
                alert('Erro ao verificar limite de uso da IA');
            }
            return false;
        }
    }
}

// Função global para inicializar o indicador
window.initAIUsageIndicator = function(containerId) {
    return new AIUsageIndicator(containerId);
};

// Função global para verificar se pode usar IA
window.canUseAI = AIUsageIndicator.canUseAI;