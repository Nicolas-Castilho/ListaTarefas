using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ListaTarefas.Areas.Identity.Data;
using ListaTarefas.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ListaTarefas.Services
{
    public class NotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public NotificationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var hoje = DateOnly.FromDateTime(DateTime.Now);
                    var hojeDateTime = hoje.ToDateTime(TimeOnly.MinValue); // Converte DateOnly para DateTime, considerando meia-noite

                    var tarefas = context.Items
                        .Where(i => i.DataVencimento.Date == hojeDateTime.Date && !i.Completa) // Usando .Date para comparar apenas a data
                        .ToList();

                    // Aqui, você poderia enviar a notificação, como um e-mail ou alerta no sistema
                    foreach (var tarefa in tarefas)
                    {
                        Console.WriteLine($"Notificação: A tarefa '{tarefa.Nome}' está próxima do prazo de vencimento.");
                    }
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Executa a cada 24 horas
            }
        }
    }
    }
