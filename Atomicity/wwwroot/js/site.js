document.addEventListener('DOMContentLoaded', () => {
    const dragArea = document.getElementById('drag-area');
    const fileInput = document.getElementById('file');
    const chooseFileButton = document.getElementById('choose-file-btn');
    const filePreviewContainer = document.getElementById('file-preview-container');
    const filePreview = document.getElementById('file-preview');
    const fileNameElement = document.getElementById('file-name');
    const fileSizeElement = document.getElementById('file-size');
    const removeButton = document.getElementById('remove-file');
    const actionButton = document.getElementById('action-btn');
    const fileUploadBox = document.querySelector('.file-upload-box');

    const atomicityCheck = document.getElementById('atomicityCheck');
    const ambiguityCheck = document.getElementById('ambiguityCheck');

    let isFileChecked = false;
    let selectedFile = null;

    const allowedFileTypes = ['application/vnd.openxmlformats-officedocument.wordprocessingml.document'];

    chooseFileButton.addEventListener('click', () => {
        fileInput.click();
    });

    fileInput.addEventListener('change', () => {
        const file = fileInput.files[0];
        if (file) {
            if (isFileAllowed(file)) {
                handleFile(file);
            } else {
                alert('Будь ласка, виберіть файл формату .docx');
            }
        }
    });

    dragArea.addEventListener('dragover', (event) => {
        event.preventDefault();
        dragArea.classList.add('dragging');
    });

    dragArea.addEventListener('dragleave', () => {
        dragArea.classList.remove('dragging');
    });

    dragArea.addEventListener('drop', (event) => {
        event.preventDefault();
        dragArea.classList.remove('dragging');
        const file = event.dataTransfer.files[0];
        if (file && isFileAllowed(file)) {
            handleFile(file);
        } else {
            alert('Будь ласка, перетягніть файл формату .docx');
        }
    });

    function isFileAllowed(file) {
        return allowedFileTypes.includes(file.type);
    }

    function handleFile(file) {
        selectedFile = file;
        fileNameElement.textContent = `Назва: ${file.name}`;
        fileSizeElement.textContent = `Розмір: ${(file.size / 1024).toFixed(2)} KB`;
        filePreviewContainer.style.display = 'flex';
        fileUploadBox.style.display = 'none';

        actionButton.textContent = 'Перевірити';

        isFileChecked = false;
    }

    removeButton.addEventListener('click', () => {
        fileInput.value = '';
        filePreviewContainer.style.display = 'none';
        fileUploadBox.style.display = 'flex';
        actionButton.textContent = 'Перевірити';
        isFileChecked = false;
    });

    actionButton.addEventListener('click', () => {
        if (!isFileChecked) {
            checkFile();
            isFileChecked = true;

        } else {
            downloadFile();
        }
    });

    async function checkFile() {
        actionButton.disabled = true;
        actionButton.textContent = 'Перевірка...';

        await new Promise(resolve => setTimeout(resolve, 2000));

        console.log('Файл перевірено');

        const checkedFile = new File([selectedFile], selectedFile.name.replace(/(\.[\w\d_-]+)$/i, '_checked$1'), {
            type: selectedFile.type
        });

        selectedFile = checkedFile;

        fileNameElement.textContent = `Назва: ${selectedFile.name}`;

        actionButton.disabled = false;
        actionButton.textContent = 'Завантажити';
    }

    function downloadFile() {
        if (!selectedFile) {
            alert('Будь ласка, виберіть файл для завантаження.');
            return;
        }

        const formData = new FormData();
        formData.append('file', selectedFile);
        formData.append('atomicityCheck', atomicityCheck.checked);
        formData.append('ambiguityCheck', ambiguityCheck.checked);

        fetch('/Pages/UploadDocument', {
            method: 'POST',
            body: formData,
        })
            .then(response => response.blob())
            .then(blob => {
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = selectedFile.name;
                a.click();
                URL.revokeObjectURL(url);
            })
            .catch(error => {
                console.error('Помилка завантаження файлу:', error);
                alert('Сталася помилка при завантаженні файлу.');
            });
    }

    atomicityCheck.addEventListener('change', () => {
        if (actionButton.textContent === 'Перевірка...') {
            atomicityCheck.checked = !atomicityCheck.checked;
        } else {
            preventUnchecking(atomicityCheck, ambiguityCheck);
        }
    });

    ambiguityCheck.addEventListener('change', () => {
        if (actionButton.textContent === 'Перевірка...') {
            ambiguityCheck.checked = !ambiguityCheck.checked;
        } else {
            preventUnchecking(ambiguityCheck, atomicityCheck);
        }
    });

    
    function preventUnchecking(checkbox1, checkbox2) {
        if (!checkbox2.checked) {
            checkbox1.checked = true; 
        }
    }

    window.addEventListener('resize', updateLayout);

    function updateLayout() {
        const isLandscape = window.innerWidth > window.innerHeight;
        if (isLandscape) {
            fileUploadBox.style.flexDirection = 'row';
        } else {
            fileUploadBox.style.flexDirection = 'column';
        }
    }
});
