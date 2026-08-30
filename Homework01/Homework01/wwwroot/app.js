const resultBox = document.getElementById('result');

document.getElementById('btnGetAll').addEventListener('click', getAllUsers);
document.getElementById('btnGetById').addEventListener('click', getUserById);

async function getAllUsers() {
    resultBox.className = '';
    try {
        const response = await fetch('/api/users');
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const users = await response.json();
        resultBox.textContent = users.map(u => `[${u.id}] ${u.name}`).join('\n');
    } catch (err) {
        resultBox.className = 'error';
        resultBox.textContent = `Error: ${err.message}`;
    }
}

async function getUserById() {
    resultBox.className = '';
    const id = document.getElementById('userId').value;

    if (id === '') {
        resultBox.textContent = 'Please enter an ID.';
        return;
    }

    if (parseInt(id) < 1) {
        resultBox.className = 'error';
        resultBox.textContent = 'ID must be 1 or greater.';
        return;
    }

    try {
        const response = await fetch(`/api/users/${id}`);
        if (!response.ok) {
            const body = await response.json().catch(() => ({}));
            throw new Error(body.message || `HTTP ${response.status}`);
        }
        const user = await response.json();
        resultBox.textContent = `[${user.id}] ${user.name}`;
    } catch (err) {
        resultBox.className = 'error';
        resultBox.textContent = `Error: ${err.message}`;
    }
}
