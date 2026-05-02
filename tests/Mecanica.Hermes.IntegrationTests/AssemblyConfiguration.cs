// Os testes de integração compartilham uma instância PostgreSQL via Testcontainers e definem
// variáveis de ambiente em escopo de processo. A execução paralela causa race conditions
// nas env vars e conflitos de estado no banco.

[assembly: CollectionBehavior(DisableTestParallelization = true)]
