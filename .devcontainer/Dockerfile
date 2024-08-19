FROM mcr.microsoft.com/devcontainers/dotnet:8.0-jammy

RUN apt-get update -y && \
    apt-get install -y libxml2-utils

# Persisting bash history between container rebuilds
RUN SNIPPET="export PROMPT_COMMAND='history -a' && export HISTFILE=/commandhistory/.bash_history" \
    && echo "$SNIPPET" >> "/root/.bashrc"